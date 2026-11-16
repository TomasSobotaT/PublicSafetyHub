# PublicSafetyHub

Základní kostra samostatného řešení pro .NET 10. Projekty zatím nemají projektové ani balíčkové reference.

| Projekt | Účel |
| --- | --- |
| PublicSafetyHub.Host | ASP.NET Core API, kontrolery a spuštění aplikace. |
| PublicSafetyHub.Application | Aplikační služby, rozhraní, mapování a registrace DI. |
| PublicSafetyHub.Base | Základní entity, výsledky, filtry, specifikace a kontext uživatele. |
| PublicSafetyHub.Data | Databázové entity, kontext, konfigurace, migrace a repozitáře. |
| PublicSafetyHub.Models | DTO, požadavky, odpovědi a vstupní filtry. |
| PublicSafetyHub.Integration | Typovaní klienti a integrace externích služeb. |
| PublicSafetyHub.AI | Služby a konfigurace pro AI. |
| PublicSafetyHub.Elasticsearch | Rezerva pro vyhledávání, konfigurace a repozitáře. |
| PublicSafetyHub.NSwagClient | Rezerva pro budoucí generovaný klient. |
| PublicSafetyHub.Tests | Jednotkové a integrační testy, zatím bez testovacího frameworku. |
| PublicSafetyHub.Mobile | Prázdný projekt pro budoucí mobilní aplikaci. |
| PublicSafetyHub.Blazor | Prázdný projekt pro budoucí frontend. |

Mobile a Blazor jsou prozatím prázdné knihovny bez MAUI a Blazor závislostí. Jejich SDK a balíčky se nastaví při zahájení implementace těchto aplikací. NSwag klient se zatím negeneruje.

Prázdné složky obsahují `.gitkeep`, aby se zachovaly při verzování. Host obsahuje pouze spuštění API bez endpointů; ukázka WeatherForecast byla odstraněna.

Sestavení: `dotnet build PublicSafetyHub.slnx`
