# PublicSafetyHub

Základní kostra samostatného řešení pro .NET 10. Datová vrstva používá EF Core, ASP.NET Core Identity a PostgreSQL.

## Spuštění po stažení z GitHubu

Předpoklady: .NET 10 SDK a aktuální Docker Desktop s dokončeným úvodním nastavením a podporou Linux containers (na Windows WSL 2). Ve Visual Studiu zvol jako startup projekt PublicSafetyHub.Host, konfiguraci Debug a profil https; stiskni F5. Nebo v kořeni řešení spusť:

```powershell
dotnet run --project PublicSafetyHub.Host
```

Debug build na Windows spustí Docker Desktop přes `docker desktop start`, poté databázi přes Compose. API při prvním startu vytvoří lokální schéma a OAuth klienty. Výchozí profil je HTTPS; Swagger je na https://localhost:7102/swagger. Ruční vytvoření databáze, kontejneru ani secrets není potřeba. Při prvním použití HTTPS může být nutné jednorázově důvěřovat vývojovému certifikátu přes `dotnet dev-certs https --trust`.

Na Linuxu musí před spuštěním běžet Docker Engine s Compose pluginem. Stažení image při prvním buildu vyžaduje internet. Pokud nenaběhne Docker/WSL backend, nejprve dokonči jeho nastavení v Docker Desktopu.

| Projekt | Účel |
| --- | --- |
| PublicSafetyHub.Host | ASP.NET Core API, kontrolery a spuštění aplikace. |
| PublicSafetyHub.Application | Aplikační služby, rozhraní, mapování a registrace DI. |
| PublicSafetyHub.Core | Konkrétní doménové entity včetně UserEntity, reference na Base. |
| PublicSafetyHub.Base | Základní entity a repozitáře, výsledky, stránkování, specifikace a kontext uživatele; bez reference na Core nebo Data. |
| PublicSafetyHub.Data | Databázový kontext, konfigurace, migrace a repozitáře; entity používá z Core. |
| PublicSafetyHub.Models | Modely podle domén, např. `Auth/Requests` a `Auth/Responses`. |
| PublicSafetyHub.Integration | Typovaní klienti a integrace externích služeb. |
| PublicSafetyHub.AI | Služby a konfigurace pro AI. |
| PublicSafetyHub.Elasticsearch | Rezerva pro vyhledávání, konfigurace a repozitáře. |
| PublicSafetyHub.NSwagClient | Rezerva pro budoucí generovaný klient. |
| PublicSafetyHub.Tests | Jednotkové a integrační testy, zatím bez testovacího frameworku. |
| PublicSafetyHub.Mobile | Prázdný projekt pro budoucí mobilní aplikaci. |
| PublicSafetyHub.Blazor | Prázdný projekt pro budoucí frontend. |

Mobile a Blazor jsou prozatím prázdné knihovny bez MAUI a Blazor závislostí. Jejich SDK a balíčky se nastaví při zahájení implementace těchto aplikací. NSwag klient se zatím negeneruje.

Host obsahuje verzované auth API; ukázka WeatherForecast byla odstraněna. Aktuální pravidla kódu jsou v [CONVENTIONS.md](CONVENTIONS.md).

Sestavení: `dotnet build PublicSafetyHub.slnx`

## Databáze

`PublicSafetyHubDbContext` ve vrstvě Data je společný pro Identity i budoucí aplikační entity. Dědí z `IdentityDbContext<UserEntity, IdentityRole<int>, int>` a načítá konfigurace přes `ApplyConfigurationsFromAssembly`. Služby používají `IPublicSafetyHubDbContext` z Data/Context/Interfaces, které DI vrací jako stejnou scoped instanci konkrétního kontextu. Uživatelé a role mají klíče typu `int` podle inspiračního projektu.

`UserEntity` je ve vrstvě Core v namespace `PublicSafetyHub.Core.Entities`. Core referencují Data, Application a Host; Base ani Models jej nereferencují. Základní entity a jejich rozhraní jsou v Base/Entities/Base; Core z nich vychází přes referenci na Base. Data referencuje Core i Base kvůli konkrétním entitám a společným repozitářům. Registraci kontextu s PostgreSQL a OpenIddict zajišťuje metoda `AddDatabase(configuration)` v Host/BuilderExtensions/DatabaseServiceCollectionExtension.cs. Connection string `DefaultConnection` se načítá uvnitř této metody.

Debug build Hostu automaticky spustí PostgreSQL 18 přes Docker Compose a počká na úspěšný healthcheck. Spuštění je MSBuild target v Host/Directory.Build.targets; v C# není žádný proces pro spouštění Dockeru. Připojení je přímo v appsettings.Development.json. Na Windows se Docker Desktop automaticky spouští před Compose; musí mít funkční Linux/WSL backend. Při prvním startu `EnsureCreatedAsync()` vytvoří schéma Identity a OpenIddict. Při dalších startech zůstávají data v pojmenovaném Docker volume `publicsafetyhub_postgres-data`. Do databáze se při startu zaregistrují chybějící OAuth klienti z `AuthOptions:ClientIds`.

Spusť API běžně ve Visual Studiu s profilem `https` nebo příkazem `dotnet run --project PublicSafetyHub.Host --launch-profile https`. Databáze je přístupná také z databázového klienta:

| Nastavení | Hodnota |
| --- | --- |
| Host | `localhost` |
| Port | `5433` |
| Databáze | `PublicSafetyHub` |
| Uživatel | `publicsafetyhub` |
| Heslo | `publicsafetyhub_dev` (pouze lokální vývoj). |

Jméno databáze, uživatel, vývojové heslo a port jsou přímo v compose.yaml; appsettings.Development.json obsahuje odpovídající connection string. Jde pouze o lokální vývojové údaje; produkční připojení nadále patří do secrets. Zastavení API kontejner nevypíná, takže se k databázi můžeš dál připojovat; zastavíš ho přes `docker stop publicsafetyhub-db`. Další Debug build jej znovu spustí. Visual Studio má pro tento profil vypnuté přeskočení buildu při nezměněném kódu.

Start kontejneru řídí Debug MSBuild target; vypneš jej přes `-p:StartDevelopmentDatabase=false`. Release build Docker nespouští. `dotnet run --no-build` build přeskočí a předpokládá již běžící databázi. Sekce `DevelopmentDatabase:Enabled` zapíná pouze vytvoření lokálního schématu při startu API v Development. Pro vlastní databázi vypni automatický start kontejneru i tvorbu schématu a nastav connection string přes secrets nebo prostředí. Connection string se v C# nepřepisuje.

Migrace zatím nejsou vytvořené. `EnsureCreatedAsync()` pouze inicializuje prázdnou vývojovou databázi, existující schéma při změně modelu neaktualizuje. Správu změn schématu bude potřeba řešit migracemi před nasazením. Pro vlastní připravenou databázi nastav připojení například takto:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=PublicSafetyHub;Username=YOUR_USER;Password=YOUR_PASSWORD" --project PublicSafetyHub.Host
```

## Auth API v1

| Metoda | Endpoint | Účel | Úspěch |
| --- | --- | --- | --- |
| POST | `/api/v1/auth/register` | Registrace e-mailu a dvou shodných zadání hesla (JSON). | 200 |
| POST | `/api/v1/auth/token` | Přihlášení nebo refresh (OAuth formulář). | 200 |
| POST | `/api/v1/auth/logout` | Zrušení aktuální relace, vyžaduje bearer token. | 200 |
| DELETE | `/api/v1/auth/account` | Soft delete vlastního účtu s ověřením hesla (JSON), vyžaduje bearer token. | 200 |

Registrace přijímá `RegisterUserRequest` s poli `email`, `password` a `confirmPassword`. Obě zadání hesla jsou povinná a musí být totožná; při neshodě se účet nevytvoří a API vrátí 400. Heslo musí mít 12–128 znaků, velké a malé písmeno, číslici a speciální znak. E-mail je zároveň přihlašovací jméno. Zatím se neposílají potvrzovací e-maily. Registrace nepřiděluje žádné role.

Přihlášení používá `Content-Type: application/x-www-form-urlencoded`:

```text
grant_type=password
client_id=public-safety-hub
username=user@example.com
password=YOUR_PASSWORD
scope=offline_access public-safety-hub-api
```

Úspěšná odpověď obsahuje `access_token`, `token_type`, `expires_in`, případně `scope` a `refresh_token`. Pro vydání refresh tokenu je potřeba rozsah `offline_access`. Access token je podepsaný JWT, výchozí platnost 15 minut. Refresh token je chráněný a spravovaný OpenIddict, výchozí platnost 14 dní.

Refresh na stejný endpoint:

```text
grant_type=refresh_token
client_id=public-safety-hub
refresh_token=YOUR_REFRESH_TOKEN
```

`TokenAsync` je společný OAuth endpoint pro login a obnovu. Controller pouze zavolá `TokenService`, který využije požadavek ověřený OpenIddict a předá operaci do `AuthService`. Formulář neparsujeme podruhé vlastním modelem; Swagger ho popisuje přes `TokenOperationTransformer`.

`AuthService`, `TokenService` a `AuthClientInitializer` jsou v Application/Services, rozhraní služeb v Application/Services/Interfaces. Přístup k aktuálnímu HTTP požadavku zapouzdřuje jediné rozhraní `IHttpUserContext` s implementací `HttpUserContext` v Base/UserContext. Sdílené `AuthOptions` a `AuthConstants` jsou v Base/Auth. Služby získávají ID uživatele, relaci, OAuth požadavek a ověřenou refresh relaci přes metody kontextu. `IHttpContextAccessor` se injektuje pouze do této společné implementace. Registrace služeb Base a Application jsou společně v `DependencyInjectionExtension.AddDependencyInjection()`. Načítání a validace AuthOptions i konfigurace certifikátů patří do `AuthServiceCollectionExtension.AddAuthServices(configuration, environment)`.

Všechny registrační extension metody jsou společně v Host/BuilderExtensions, včetně registrací Base, Application a Data. EF konfigurace entit jsou v Data/Configurations a `TokenOperationTransformer` má samostatný soubor v Host/BuilderExtensions.

Po obnově ulož nový refresh token a starý zahoď. Obnovu pro jednu relaci prováděj pouze jedním souběžným požadavkem. Volitelný `scope` při obnově smí oprávnění pouze zúžit, nikoliv rozšířit.

Logout a smazání účtu vyžadují `Authorization: Bearer YOUR_ACCESS_TOKEN`. Smazání přijímá JSON `{ "password": "YOUR_PASSWORD" }`. Logout ruší jen aktuální relaci; smazání zneplatňuje všechny relace a blokuje nové přihlášení i refresh. E-mail zůstává obsazený i po soft delete.

REST chyby používají HTTP status a `ProblemDetails` s jednou anglickou zprávou v `title`, bez vlastních interních kódů a bez slovníků chyb polí. Validační chyby vracejí HTTP 400; při více chybách se vrací první zpráva. Token endpoint používá standardní OAuth chyby. Po pěti chybných heslech se účet na 15 minut zablokuje; auth endpointy mají také limit 30 požadavků za minutu na IP.

Všechny úspěšné odpovědi mají HTTP 200. Služba vrací přímo DTO nebo `NoValueResult.Value`; controller pouze vrací `result.ToActionResult()` bez zadávání statusu. Logout a smazání vracejí 200 bez těla. Vytváření tokenové odpovědi je protokolová operace OpenIddict.

## Swagger a nastavení

V prostředí Development je Swagger UI na `/swagger` a OpenAPI v1 na `/openapi/v1.json`. Dokumentace nahrazuje parametr verze v cestách, popisuje formulář pro token endpoint a umožňuje vložit access token přes **Authorize** pro chráněné operace.

Spuštění po přípravě databáze: `dotnet run --project PublicSafetyHub.Host --launch-profile https`. Výchozí issuer je `https://localhost:7102/`, musí odpovídat veřejné adrese serveru. Pro produkci nastav `AuthOptions:Issuer`, `AuthOptions:SigningCertificatePath`, `AuthOptions:EncryptionCertificatePath` a odpovídající `AuthOptions:SigningCertificatePassword` / `AuthOptions:EncryptionCertificatePassword` přes secrets. Vývojové certifikáty se používají pouze v Development. HTTPS je povinné.

Povolené klienty nastavuje `AuthOptions:ClientIds`. Odebraný klient již nemůže provést login ani refresh. Veřejné klientské ID není tajemství a pro vlastní Blazor/MAUI se nepoužívá sdílené heslo klienta.

Na přání uživatele nejsou součástí změny žádné testy. Sestavení samo neověřuje běh proti PostgreSQL.

Nastavení autentizace načítá `AddConfiguration<IAuthOptions, AuthOptions>(configuration, ...)` ze sekce `AuthOptions`. Služby injektují přímo `IAuthOptions`. Případné user secrets a proměnné prostředí používají nový prefix `AuthOptions:` / `AuthOptions__` místo původního `Auth:` / `Auth__`.

Validaci registrace a smazání účtu zajišťují FluentValidation validátory v Application/Validators/Auth. Volají je přímo služby před prací s daty; vracejí první anglickou zprávu přes ApplicationResult jako HTTP 400. Request DTO neobsahují validační atributy. Nečitelný JSON a chybějící tělo požadavku zpracovává standardně ASP.NET Core.

## Repozitáře

Z PortfolioManager V2 vychází Base/Repositories: `BaseRepository<TEntity, T>`, `ExtendedBaseRepository<TEntity, T>` a jejich rozhraní. Konkrétní `UserRepository`/`IUserRepository` jsou v Data. Repozitáře sdílejí scoped DbContext, ale Base pracuje pouze s obecným EF DbContext a nezná Data.

CRUD změny se ukládají přes `Commit()` nebo `CommitAsync()`; výsledkem je počet uložených stavových záznamů EF. Delete nastavuje `IsDeleted`. Smazané záznamy vynechává EF query filtr v konfiguraci entity, který platí i pro GetDbSet a GetQueryable. Výjimku lze explicitně provést přes `IgnoreQueryFilters()`.

`Specification<TEntity>` umožňuje zadat `Criteria`, `Includes`, `OrderBy` a `OrderDescending`. `GetPagedAsync(pageNumber, pageSize, specification)` očekává platné parametry ověřené aplikační validací; vrací položky i celkový počet a počet stránek. Výchozí a doplňující řazení podle ID stabilizuje stránkování.

AuthService používá IUserRepository pro čtení. Hesla a vytváření účtu nadále spravuje Identity; smazání účtu zahrnuje také změnu security stamp a zneplatnění všech relací/tokenů v AuthService.

UserRepository neprovádí normalizaci vstupů: GetUserByEmailAsync přijímá již normalizovaný e-mail z AuthService. Obsazenost e-mailu při registraci se kontroluje i mezi smazanými účty přes IgnoreQueryFilters.
