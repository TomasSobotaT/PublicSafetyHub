# Konvence PublicSafetyHub

Tento dokument zachycuje zadání z `../StatniSlozky.txt`, konvence z `../code.txt` a následná upřesnění uživatele. Při rozporu platí nejnovější pokyn uživatele a pravidla zde.

## Aktuální upřesnění

- Vlastní entity, DTO a ostatní typy piš jako běžné třídy, ne `record` ani `record struct`. Nepoužívej `sealed` třídy.
- Názvy requestů obsahují operaci i entitu: `RegisterUserRequest`, `DeleteUserRequest` a obdobně pro další operace.
- Parametry a proměnné request DTO pojmenovávej podle celého názvu typu v camelCase: `RegisterUserRequest registerUserRequest`, `DeleteUserRequest deleteUserRequest`, `OpenIddictRequest openIddictRequest`. Nepoužívej obecné `request`. Platí shodně pro rozhraní, controllery, služby i předávání argumentů.
- Metody registrace a mazání uživatele pojmenovávej `RegisterUserAsync` a `DeleteUserAsync`, shodně v controlleru, rozhraní i službě.
- Registrace vyžaduje `Password` a `ConfirmPassword`; hesla musí být totožná. Shodu kontroluje FluentValidation validátor volaný aplikační službou před vytvořením účtu. Potvrzení hesla neukládej.
- Pro vlastní časová pole používej `DateTime` a `DateTime?`. Aktuální čas vytvářej přes `DateTime.Now` podle lokálního času serveru. Vlastní lokální časová pole v PostgreSQL mapuj jako `timestamp without time zone`. Typy a časové chování vlastněné frameworky nebo knihovnami se nepřepisují.
- Výsledkový typ se jmenuje `ApplicationResult<T>` a patří do Base.
- Chybové zprávy, validační zprávy a texty výjimek jsou anglicky.
- Nevymýšlej vlastní interní chybové kódy jako `auth.email_exists`. Veřejné REST odpovědi používají HTTP status, v chybovém těle `ProblemDetails.status`.
- Validační chyba obsahuje zatím pouze jednu anglickou zprávu: `ResultHelper.Validation("Message.")`. Nepřidávej slovníky ani seznamy chyb jednotlivých polí. Při více aplikačních validačních chybách se vrací první zpráva, včetně chyb Identity. Chyby čtení JSON a chybějícího těla řeší standardní odpověď ASP.NET Core.
- Standardní názvy chyb OAuth (`invalid_grant`, `invalid_client`, `invalid_scope` apod.) jsou součástí protokolu OpenIddict, nikoli vlastní aplikační kódy. Token endpoint zachovává standardní OAuth odpovědi a příslušné HTTP statusy.
- Zatím nepiš žádné testy a nepřidávej testovací frameworky. Ověřuj sestavení a kontroluj kód.

## Vrstvy a databáze

- Jeden společný `PublicSafetyHubDbContext` pro Identity, OpenIddict i další aplikační data. Služby používají `IPublicSafetyHubDbContext` z Data/Context/Interfaces; rozhraní i konkrétní kontext v DI odkazují na stejnou scoped instanci. Identity a OpenIddict používají konkrétní typ kontextu.
- Core: konkrétní doménové entity včetně `UserEntity`, namespace `PublicSafetyHub.Core.Entities`, reference na Base kvůli společným entitám. Base: základní entity, generické repozitáře, výsledky, stránkování, specifikace, kontext uživatele a sdílené auth nastavení; bez reference na Core a Data. Models: DTO bez reference na Core. Data: kontext, EF konfigurace a konkrétní repozitáře, reference na Core a Base. Application: služby a validátory. Host: kontrolery, start aplikace a nastavení HTTP/OpenAPI.
- V Models je nejprve doména a až pod ní složky `Requests` a `Responses`: například `PublicSafetyHub.Models/Auth/Requests/RegisterUserRequest.cs` a `PublicSafetyHub.Models/Auth/Responses/UserResponse.cs`. Namespace odpovídá této struktuře. Nevytvářej kořenové složky Requests/Responses/Dtos/Filters pro všechny domény dohromady.
- EF Core 10 a PostgreSQL přes Npgsql; entity konfiguruj přes `IEntityTypeConfiguration<T>` a načítej přes `ApplyConfigurationsFromAssembly`.
- Používej soft delete. `UserEntity` má `CreatedAt`, nullable `UpdatedAt` a příznak `IsDeleted`. Samostatné `DeletedAt` nemá; smazání nastaví `IsDeleted` a čas této změny je v `UpdatedAt`. DbContext při uložení automaticky doplňuje `CreatedAt` při vytvoření a `UpdatedAt` při změně přes `DateTime.Now`. `CreatedAt` se při změně nepřepisuje. Databázové enumy ukládej jako string.
- Při implementaci repozitářů použij společný základ pro CRUD, stránkování a filtrování a unit of work na repozitáři.
- Základní entity a rozhraní jsou v Base/Entities/Base podle PortfolioManager: `BaseEntity<T> : IBaseEntity<T>` má `Id` a `IsDeleted`; `ExtendedBaseEntity<T> : BaseEntity<T>, IExtendedBaseEntity<T>` přidává `CreatedAt` a `UpdatedAt`. `UserEntity : IdentityUser<int>, IExtendedBaseEntity<int>` je v Core a má také `IsBlocked`. Vlastnosti CreatedBy/UpdatedBy se nepoužívají. DbContext automaticky nastavuje a mapuje časy přes negenerické `IExtendedBaseEntity`.

## Výsledky a HTTP

- Aplikační služby vracejí `ApplicationResult<T>`. Úspěšná data mohou vracet přímo přes implicitní konverzi; očekávané chyby přes `ResultHelper`.
- Aplikační výsledky nezávisí na ASP.NET Core ani na HTTP. `ErrorType` slouží pouze k internímu rozlišení druhu chyby; Host jej převádí na HTTP status a nevystavuje jej jako vlastní veřejný kód.
- Všechny úspěšné odpovědi používají HTTP 200, včetně registrace, logoutu a smazání účtu. HTTP 201 ani 204 nepoužívej. Služba vrací přímo DTO nebo `NoValueResult.Value`, pokud nemá vracet data; nepotřebuje rozlišovat typ úspěchu ani zadávat HTTP status.
- Společná třída `ResultExtension` s metodou `ToActionResult()` patří do Base/Extensions. Host ji používá bez předávání statusu z controlleru. Úspěch automaticky převádí na HTTP 200 s DTO nebo bez těla pro `NoValueResult`. Chyby používají `ProblemDetails`, zprávu v `title`, bez dodatečných chyb polí. Validační chyby vracejí HTTP 400.
- HTTP statusy: 200 úspěch, 400 validace a očekávané konflikty (např. existující e-mail), 401 nepřihlášený uživatel, 403 chybějící oprávnění, 404 nenalezeno, 429 překročení limitu, 500 neočekávaná chyba. HTTP 409 nepoužívej.
- Vlastní centrální exception handler zatím nepoužívej. Neočekávané výjimky ponech standardnímu chování hostu; negarantuj pro ně tělo ProblemDetails. Autentizační a autorizační chyby řeší příslušný middleware.
- Kontrolery zůstávají tenké: zavolají službu a vrátí výsledek. `AuthService`, `TokenService` a `AuthClientInitializer` patří do Application/Services, jejich rozhraní do Application/Services/Interfaces. V Hostu nezakládej složku Services. TokenService je integrační služba OpenIddict vracející jeho SignIn/Forbid výsledek; běžné aplikační služby používají ApplicationResult. Controller nerozhoduje mezi loginem a refreshem ani neověřuje klienta či rozsahy.
- API verzuj pomocí `Asp.Versioning.Mvc`, první verze je v1. Návratové typy a statusy popisuj v OpenAPI pro budoucího NSwag klienta.
- Společné atributy dávej na controller pouze tehdy, pokud platí pro všechny jeho akce. Společný HTTP 200 bez těla může konkrétní akce přepsat přes `ProducesResponseType<Dto>(200)`. Ostatní statusy se dědí a přidávají, proto nedávej na controller chyby, které některá akce nevrací. V AuthController jsou společné 200 a 429; rozdílné chyby a konkrétní DTO jsou na akcích. OAuth chyby token endpointu nesmějí být v dokumentaci nahrazené ProblemDetails.
- Autorizace může být výchozí na controlleru; veřejné akce musí mít explicitní `AllowAnonymous`. OpenAPI vyžaduje bearer token pouze na akcích bez `AllowAnonymous`; veřejným akcím automaticky nepřidávej 401/403.
- Swagger UI používá dokument pro danou verzi (`/openapi/v1.json`), cesty se skutečnou verzí a bearer autentizaci pro chráněné operace. OAuth požadavky jsou formulářové, nikoli JSON.
- Formulář token endpointu parsuje a ověřuje OpenIddict. Nevytvářej druhý formulářový model jen kvůli Swaggeru; požadavek popisuje OpenAPI operation transformer.

## Styl a další patterny

- Používej `var`, popisné nepříliš dlouhé názvy a čitelný kód v postupných krocích.
- Před příkazem `if` a `return` nech právě jeden prázdný řádek, pokud před ním už je jiný příkaz. Hned za otevírací `{` prázdný řádek nepatří, ani když následuje `if` nebo `return`. Výrazy s operátorem `??` piš na jeden řádek včetně náhradní hodnoty nebo `throw`; nezalamuj je před ani za `??`.
- `if` vždy se složenými závorkami. Async metody mají příponu `Async`. Ve vlastních metodách, controllerech ani službách nepoužívej `CancellationToken` a při volání knihoven vol overload bez předaného tokenu. Pokud token vyžaduje signatura frameworkového rozhraní nebo callbacku, ponech povinný parametr jako `_`, nepoužívej ho a nepředávej dál.
- Používej primary konstruktory, rozhraní a DI. Všechny registrační a konfigurační extension metody mimo Program.cs patří do Host/BuilderExtensions s namespace `PublicSafetyHub.Host.BuilderExtensions`, včetně registrací Base, Application a Data. Skutečné EF konfigurace entit zůstávají v Data/Configurations. `TokenOperationTransformer` má samostatný soubor v Host/BuilderExtensions. Delší DI konstruktory piš přes více řádků.
- Typovaní HTTP klienti, kontext uživatele, specification pattern pro filtrování, stránkování a řazení.
- `IHttpContextAccessor` injektuj pouze do `HttpUserContext`, nikoli do jednotlivých služeb. Rozhraní i implementace kontextu patří do Base/UserContext. Tokenová služba používá `IHttpUserContext` a jeho konkrétní metody (`GetUserId()`, `GetSessionId()`, `GetTokenRequest()`, `GetRefreshTokenPrincipalAsync()`). Rozhraní nevystavuje samotný `HttpContext`. Middleware může nadále používat kontext předaný ASP.NET Core.
- Všechny služby používají jediné rozhraní `IHttpUserContext` z Base pro údaje uživatele, relace a OAuth požadavku. Registruj je jako scoped s implementací `HttpUserContext`; další rozhraní `IUserContext` nevytvářej.
- Registrace uživatelského kontextu, aplikačních služeb a inicializace klientů jsou v `DependencyInjectionExtension.AddDependencyInjection()`. Načítání a validace AuthOptions i konfigurace certifikátů patří do `AuthServiceCollectionExtension.AddAuthServices(configuration, environment)`. Databázi registruje `DatabaseServiceCollectionExtension.AddDatabase(configuration)` a connection string si načítá uvnitř. Base ani Application nesmějí referencovat Host.
- Pro aplikační mapování používej AutoMapper, jakmile je potřeba netriviální mapování.
- Připojení, hesla a klíče patří lokálně do user secrets, při nasazení do secrets prostředí/GitHub Actions. Žádné skutečné secrets do zdrojového kódu.
- Aktuálně implementujeme jen API. Blazor, MAUI a generování NSwag klienta zůstávají na později.

- Názvy vlastních extension tříd a jejich souborů jsou v jednotném čísle s příponou `Extension` (např. `ResultExtension`, `DependencyInjectionExtension`). Složky a odpovídající namespaces zůstávají v množném čísle (`Extensions`, `BuilderExtensions`).
- Obecná registrační metoda `AddConfiguration<TInterface, TConfiguration>(configuration)` je v Base/Extensions/ServiceCollectionExtension.cs. Načítá povinnou sekci podle přesného názvu třídy `TConfiguration` a zaregistruje naplněnou instanci jako singleton pod `TInterface`. Podporuje vlastnosti, pole hodnot a vnořené objekty přes standardní configuration binder. Hodnoty jsou snímkem při registraci, bez automatického načítání změn. Konkrétní registrace nadále patří do Host/BuilderExtensions.
- Autentizační nastavení používá `AuthOptions : IAuthOptions` a registraci `AddConfiguration<IAuthOptions, AuthOptions>`; služby injektují přímo `IAuthOptions`. Sekce konfigurace se jmenuje `AuthOptions`, secrets používají prefix `AuthOptions:` a proměnné prostředí `AuthOptions__`. Volitelný callback extension provede kontrolu nastavení před registrací; zachovej kontrolu HTTPS issueru, klientů, životnosti tokenů a produkčních certifikátů.

- Lokalizaci požadavků zatím nepoužívej: neregistruj `RequestLocalizationOptions` ani `UseRequestLocalization()`.

- Validátory požadavků patří do Application/Validators podle domény a používají FluentValidation. Request DTO nemají validační DataAnnotations ani C# `required`; povinné hodnoty ověřují validátory. Služby injektují `IValidator<T>` a volají `ValidateAsync` bez tokenu před prací s daty. První chybu převedou přes `ResultHelper.Validation`. Validátory registruj skenováním assembly v Host/BuilderExtensions/DependencyInjectionExtension. Nevracej vlastní `InvalidModelStateResponseFactory`; implicitní Required pro non-nullable reference v MVC je vypnuté.

- Všechny projekty používají `<Nullable>disable</Nullable>`. Nepiš nullable anotace referenčních typů (např. `string?`) ani null-forgiving operátor. Nullable hodnotové typy jako `int?` a `DateTime?` zachovej podle významu dat; kontroly null za běhu a FluentValidation zůstávají.

- Společné `BaseRepository<TEntity, T>` a `ExtendedBaseRepository<TEntity, T>` s rozhraními patří do Base/Repositories podle PortfolioManager V2. Používají obecný EF `DbContext`, nikdy datový kontext z Data. Konkrétní repozitáře a rozhraní (zatím `UserRepository`/`IUserRepository`) patří do Data/Repositories a Data/Repositories/Interfaces; registrace do Host/BuilderExtensions/DependencyInjectionExtension.
- Repo operace Add/Update/Delete jen připravují změny. Uložení celé sdílené scoped instance zajišťuje `Commit`/`CommitAsync`, jejich int výsledek je počet uložených stavových záznamů EF. Delete je soft delete. Výchozí vynechání smazaných zajišťuje `HasQueryFilter(entity => !entity.IsDeleted)` v EF konfiguraci konkrétní entity, nikoli podmínky v repozitáři. Parametry includeDeleted se nepoužívají. Výjimečné načtení smazaných prováděj explicitně přes `IgnoreQueryFilters()`; i GetDbSet a GetQueryable standardně respektují query filtry. `ISpecification`/`Specification` podporují filtr, Include a řazení; `GetPagedAsync` vrací PagedResult a stabilní doplňující řazení podle ID. Platnost parametrů stránkování ověřuje aplikační validace, nikoli repozitář.
- AuthService čte uživatele přes IUserRepository, při registraci hledá i smazané účty kvůli obsazenému e-mailu. Vytvoření účtu, hesla, lockout a security stamp zůstávají přes Identity UserManager/SignInManager; tokeny a relace přes OpenIddict managery. Repository Delete nenahrazuje celý postup smazání účtu a zneplatnění tokenů v AuthService.

- Repozitáře obsahují jen databázové operace a nezbytnou datovou logiku. Nenormalizují ani neořezávají vstupy a neinjektují ILookupNormalizer. Normalizaci e-mailu provádí AuthService přes UserManager.NormalizeEmail; UserRepository.GetUserByEmailAsync jen dotazuje NormalizedEmail. Registrace při kontrole obsazeného e-mailu výslovně obchází query filtr, aby zůstal obsazený i po soft delete.

- Vlastní metody konkrétních repozitářů pojmenovávej celým názvem včetně entity a kritéria, např. `GetUserByEmailAsync`, nikoli `GetByEmailAsync`. Názvy sjednoť v rozhraní, implementaci a voláních. Generické metody společného repozitáře zůstávají obecné (`GetAsync`, `GetAllAsync` apod.).
