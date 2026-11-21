# Authorization Code Flow s PKCE

Server používá OpenIddict 7.9.0 a Identity pro účty a ověření hesla. Password grant je odstraněný. Budoucí Blazor WASM i MAUI používají Authorization Code Flow s povinným PKCE **S256**, bez `client_secret`. Klientské obrazovky, externí poskytovatelé ani `/me` nejsou součástí změny.

## Registrace klientů

`AuthOptions.ClientIds` nahrazuje `Clients`. V repozitáři jsou vypnuté registrace bez callbacků: původní `public-safety-hub` pro web a samostatný `public-safety-hub-mobile`. Zapněte je teprve po doplnění skutečných adres. Příklad používá pouze ilustrační adresy:

```json
{
  "AuthOptions": {
    "Issuer": "https://identity.example.cz/",
    "Clients": [
      {
        "ClientId": "public-safety-hub",
        "Enabled": true,
        "RedirectUris": [ "https://web.example.cz/authentication/login-callback" ],
        "Scopes": [ "openid", "roles", "offline_access", "public-safety-hub-api" ]
      },
      {
        "ClientId": "public-safety-hub-mobile",
        "Enabled": true,
        "RedirectUris": [ "cz.example.publicsafetyhub:/oauth2redirect" ],
        "Scopes": [ "openid", "roles", "offline_access", "public-safety-hub-api" ]
      }
    ]
  }
}
```

Issuer musí být veřejný HTTPS origin bez další cesty, query, fragmentu a user information. Stabilní RSA privátní podpisový a 32bajtový šifrovací klíč nadále patří do user secrets nebo prostředí a musí být společné pro všechny instance. Výchozí životnosti zůstávají 15 minut a 14 dnů. Změna konfigurace vyžaduje restart.

Callbacky musí být přesné absolutní HTTPS adresy nebo nativní schémata s obrácenou doménou. HTTP, wildcardy, fragmenty a user information jsou odmítnuté. Pro MAUI zaregistrujte vlastní schéma také v OS; pokud aplikace má ověřené HTTPS app/universal links, upřednostněte je.

Inicializátor vytváří nebo aktualizuje konfigurované registrace na místě: přepíše public typ, callbacky, oprávnění i PKCE požadavek a odstraní původní password oprávnění. Zapnuté klienty opravňuje k authorization/token, authorization_code/refresh_token, response type code a příslušným scopes. Vypnutý klient má prázdná oprávnění a nemůže provést nové přihlášení ani výměnu tokenů. Pro vypnutí spravované registrace ji ponechte v konfiguraci s `Enabled=false`; pouhé odstranění položky nemaže jiné databázové registrace. Host i TokenService navíc vyžadují zapnutého konfigurovaného klienta.

Migrace nevyžaduje smazání databáze. Při ověřování nad existující DB vypněte původní `DevelopmentDatabase:Enabled`, které při startu v Development databázi samostatně resetuje.

API scope je povinný. `openid`, `roles` a `offline_access` jsou volitelné podle klientské konfigurace. OpenIddict pro `openid` a `offline_access` nevyžaduje scope permissions; aplikace proto kontroluje jejich konfigurovaný allowlist výslovně.

Blazor bude na stejném originu jako backend a nativní mobilní aplikace nepodléhá prohlížečovému CORS. Aplikace proto neregistruje CORS politiku ani CORS middleware. Přesné callbacky jsou nadále povinné pro ověření OAuth návratové adresy; s CORS nejsou propojené.

## První přihlášení bez cookies

1. OIDC knihovna klienta vytvoří PKCE verifier/challenge, `state` a `nonce`. Naviguje metodou GET na `/connect/authorize` s `client_id`, přesnou `redirect_uri`, `response_type=code`, scopes, `state`, `nonce`, `code_challenge` a `code_challenge_method=S256`.
2. OpenIddict validuje požadavek. Server uloží původní validované parametry pod náhodným 256bitovým ID nejvýše na pět minut a vrátí JSON `AuthorizationTransactionResponse` s `transactionId`. Backend nevytváří HTML ani UI; uživatelskou interakci doplní budoucí Blazor na originu issueru.
3. Password POST na `/connect/authorize` vyžaduje HTTPS, veřejný origin issueru, přesnou jednohodnotovou hlavičku `Origin` issueru, tělo `application/x-www-form-urlencoded` a platnou transakci. Pro přihlášení obsahuje přesně `transaction_id`, `intent=login`, `email`, `password`; pro registraci `intent=register` navíc `confirm_password`. Jde o síťový formát těla, nikoli serverový HTML formulář. Chybějící, `null` i cizí Origin, query parametry, duplicitní pole nebo jakékoli další pole jsou odmítnuté. Limit těla je 16 KiB. Pravidla pro Origin se nevztahují na úvodní navigaci GET z klienta.
4. Transakce se atomicky spotřebuje před ověřením hesla. Middleware obnoví OpenIddictu původní protokolový formulář; klient, callback, scopes, state, nonce i PKCE se tak nemohou změnit. Heslo předá pouze aktuální controller akci, nikdy pokračovacímu OAuth požadavku, URL ani transakčnímu úložišti. OpenIddict celý obnovený požadavek znovu validuje.
5. Identity `CheckPasswordSignInAsync(..., lockoutOnFailure: true)` zachová lockout a kontroly účtu, cookie nevydá. Při neúspěchu služba vrátí ApplicationResult s chybou a Host ji převede přes ToActionResult na ProblemDetails. Pro další pokus klient znovu provede původní autorizační GET a získá novou transakci; spotřebované ID už nepoužije. Po úspěchu předá principal vytvořený z databáze včetně authorization ID serverovému schématu OpenIddict; to vydá kód a protokolovou odpověď.

Všechny autentizační endpointy jsou v `AuthController`: neversionované GET/POST `/connect/authorize` mají neutrální verzi jen na příslušných akcích, ostatní API zachovává verzi 1.0. Application `AuthService` připravuje transakce, získává aktuální požadavek přes IHttpUserContext, kontroluje klienta/scopes a řídí registraci s přihlášením. Transakční úložiště je rovněž Application služba. Controller pouze převádí ApplicationResult přes ToActionResult nebo provede nezbytné OpenIddict SignIn/Forbid. Výsledky Application neobsahují MVC typy. Middleware zajišťuje HTTP ochranu před protokolovou validací; jeho chyby také používají ResultHelper/ApplicationResult a stejný převod do ProblemDetails.

Při `intent=register` server použije stávající `RegisterUserAsync` s validací včetně potvrzení hesla a požadavků Identity. Po úspěchu v témže HTTP požadavku zavolá `LoginAsync` s právě zadanými údaji a dokončí autorizaci přes OpenIddict; uživatel heslo podruhé nezadává. Registrace ani potvrzení hesla nevstupují do uložené transakce ani OAuth parametrů.

Při validační chybě server vrátí ProblemDetails z ApplicationResult, bez hesel. Nový pokus vyžaduje novou transakci z GET. Existující nebo smazaný účet nelze touto cestou přepsat ani automaticky přihlásit. Pokud účet vznikne, ale následné přihlášení neprojde kontrolami Identity, zpráva oznámí vytvoření účtu a nutnost přihlášení. Samostatný API endpoint `RegisterUser` je odstraněný; registrační metoda je soukromá část AuthService a používá se jen v rozpracované autorizaci s `intent=register`.

Nevzniká Identity cookie, session ani antiforgery cookie. CSRF ochranu password POSTu zajišťuje současně přesný Origin a náhodná serverová transakce s krátkou platností a atomickým spotřebováním. Odpovědi mají no-store a zákaz vložení do rámce. Standardní protokolová odpověď OpenIddict `response_mode=form_post` zůstává podporovaná; nejde o vlastní UI aplikace.

Bez budoucího Blazor UI zatím nelze dokončit interaktivní přihlášení pouhou navigací v prohlížeči: GET vrací JSON, nikoli přihlašovací stránku. Přihlašovací část Blazoru musí běžet na originu issueru, aby zůstala přesná kontrola Origin. Při jeho doplnění je nutné propojit autorizační navigaci, interakci a standardní callback OIDC knihovny; aktuální JSON není náhradou OIDC odpovědi. Způsob přepínání přihlášení/registrace bude čistě záležitost FE, backend už nemá show_login/show_registration.

`prompt=none` vrací `login_required`. `prompt=login` i další nová interaktivní autorizace vyžadují nové ověření uživatele. Refresh probíhá bez interakce.

Úložiště má limit 4096 transakcí na instanci, expirované položky čistí při vytváření dalších a při zaplnění vrátí 503. Restart zneplatní rozpracované pokusy. Více instancí vyžaduje směrování GET i POST na stejnou instanci **bez cookies**, například stabilním směrováním podle klientské IP. Pro obecný distribuovaný provoz je potřeba sdílená implementace s atomickým consume; cookie affinity nesplňuje požadavek na blokované cookies.

Za proxy nastavte důvěryhodné forwarded headers před routováním: aplikace musí vidět veřejné HTTPS schéma/Host a skutečnou IP pro limiter. Nedůvěřujte libovolným forwarded hlavičkám. Na proxy i v telemetrii vypněte logování těl login/token požadavků a redigujte kódy, verifier, hesla i tokeny. Aplikace přihlašovací údaje neloguje.

## Tokeny a API

Obě adresy token endpointu zůstávají: POST `/api/v1/auth/token` a `/api/v1.0/auth/token`; discovery publikuje první. Pro výměnu kódu odešle klient `grant_type=authorization_code`, `client_id`, `code`, původní `redirect_uri` a `code_verifier`. OpenIddict kontroluje binding klienta, callback, PKCE, platnost a jednorázovost kódu. `password` grant a `plain` challenge nejsou podporované.

TokenService přebírá autentizovaný principal kódu/refresh tokenu ze serverového schématu. Zachovává protokolový kontext včetně nonce, ověřuje relaci, účet, blokaci/lockout a security stamp a obnovuje role z databáze. Stamp se nedostává do veřejných claims. Application vrací `TokenExchangeResult`; Host provádí `SignIn`/`Forbid`.

Refresh token se vydává při udělení `offline_access`. Obnova posílá pouze na token endpoint `grant_type=refresh_token`, `client_id`, `refresh_token` a případně užší `scope`. Bez scope se zachová původní množina. Rozšíření scopes je odmítnuté a platí i aktuální allowlist klienta. Rotace OpenIddict zůstává zapnutá a reuse leeway je nula.

Chráněné API má jedinou politiku `ApiAccess` s explicitním validačním schématem OpenIddict, audience a scope `public-safety-hub-api`. Token se získává jen z `Authorization: Bearer ...`; cookie, query ani body ho nenahradí. Veřejné články a registrace zůstávají anonymní. Access token patří do API, ID token identifikuje uživatele klientovi a refresh token patří pouze na token endpoint.

API `Logout` pod platným access tokenem odvolá authorization i všechny tokeny dané relace. `DeleteUser` odvolá všechny relace/tokeny uživatele a změní stamp. API nadále kontroluje token i authorization entries, takže platná JWT signatura po revokaci nestačí.

## Role v budoucím Blazor WASM

Web žádá `openid roles public-safety-hub-api` a pro obnovu také `offline_access`. Při uděleném `roles` obsahuje ID token všechny role jako hodnoty claimu `role`; více hodnot se může serializovat jako `"role": ["Editor", "Admin"]`. Access token zachovává původní kontrakt rolí i bez scope `roles`.

V budoucí registraci Blazor OIDC nastavte `options.UserOptions.RoleClaim = "role"` a přidejte uvedené default scopes. `AuthenticationState` vytvářejte z identity získané a ověřené OIDC knihovnou. Při vlastní `AccountClaimsPrincipalFactory` rozbalte každou položku JSON pole `role` do samostatného claimu typu `identity.RoleClaimType`, nikdy do jednoho claimu s textem celého pole. Jedna role znamená jeden claim, bez rolí se žádný role claim nepřidá. Neduplicujte claims, které už knihovna rozbalila.

Uživatel s Editor+Admin musí splnit `IsInRole("Editor")` i `IsInRole("Admin")` a samostatné `AuthorizeView Roles="Editor"` i `AuthorizeView Roles="Admin"`. Uživatel bez rolí nesplní žádnou z nich. Menu vyhodnocujte lokálně ze stejné identity; server vždy autorizuje operace z ověřeného access tokenu.

## Rozsah ověření a provozní akceptace

Ověření této změny používá sestavení a kontrolu kódu podle pokynů projektu. Následující scénáře jsou předávací postup pro provozní ověření, nikoli tvrzení o provedených integračních kontrolách:

- Discovery `/.well-known/openid-configuration`: authorization endpoint `/connect/authorize`, kanonický token endpoint `/api/v1/auth/token`, granty authorization_code/refresh_token, response type code, scopes openid/roles/offline_access/API a pouze S256 v `code_challenge_methods_supported`; žádný password grant. Metadata tvoří standardní OpenIddict handlery ze server options.
- Se zcela zablokovanými cookies projít přihlášení, chybný pokus a opravu hesla, query i form_post callback, výměnu kódu, kontrolu nonce/state OIDC knihovnou a refresh. Odpovědi nesmí vydat `Set-Cookie`; nová autorizace musí vyžadovat nové heslo.
- Ověřit JSON transakci GET, POST login/register, chybná/neshodná hesla, obsazený e-mail a registraci s okamžitým callbackem bez druhého zadání hesla. U každého retry ověřit nové transakční ID, odmítnutí starého ID a zachování state/nonce/PKCE. Samostatná API cesta RegisterUser už nesmí existovat. Prohlížečové scénáře vyžadují pozdější UI.
- Zamítnout chybějící/plain challenge, chybný verifier, opakovaný kód, jiného klienta/callback, expirovanou nebo souběžně použitou transakci, podvržená OAuth pole/query a chybějící/null/cizí Origin. Neplatný klient/callback nesmí získat interaktivní transakci.
- Ověřit ID token s Editor+Admin, jednou rolí, bez rolí a bez scope roles, samostatné role claims v klientovi a nepřítomnost stampu v ID/access tokenu.
- Ověřit API s Bearer tokenem a odmítnutí cookie samotné i tokenu v query/body. Po Logout musí selhat API i refresh odvolané relace, jiná relace zůstane platná; po DeleteUser selžou všechny. Smazaný, blokovaný či zamčený účet nesmí dostat nové tokeny.
- Inicializátor spustit opakovaně nad existující DB, změnit callback/scopes a vypnout klienta; ověřit aktualizaci stejného záznamu a odstranění password oprávnění bez resetu DB. Z originu společného pro Blazor a backend ověřit discovery/token/API bez cookies.

Reference: [PKCE](https://documentation.openiddict.com/configuration/proof-key-for-code-exchange), [oprávnění aplikací](https://documentation.openiddict.com/configuration/application-permissions), [claim destinations](https://documentation.openiddict.com/configuration/claim-destinations).

## User administration

Reusable OpenIddict-specific helpers live in Base/OpenIddictAuth (namespace PublicSafetyHub.Base.OpenIddictAuth); the AuthOptions configuration section retains its name.

The ApiAccess policy and Admin role protect these endpoints:

- GET /api/v1/admin/users: FilteredRequest<UserFilter> with SearchValue (case-insensitive email substring), IsBlocked, and standard validated pagination and sorting (Id, Email, CreatedAt, UpdatedAt, IsBlocked). Deleted users are excluded. Only the User DTO is projected; password hashes, stamps and other Identity internals are never returned.
- PUT /api/v1/admin/users/{id}/blocked: body {"isBlocked":true} blocks, false unblocks.
- DELETE /api/v1/admin/users/{id}: soft deletes an account.

Mutations return HTTP 200 on success, 404 for a missing/deleted user and 400 for invalid input. An administrator cannot block or delete their own account through these endpoints. Blocking or deleting updates the security stamp and revokes all authorizations and tokens within one UnitOfWork transaction. Unblocking does not reactivate revoked sessions.
