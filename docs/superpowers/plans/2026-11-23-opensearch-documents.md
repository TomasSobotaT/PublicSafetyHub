# OpenSearch Documents Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Ukládat samostatné dokumenty a interně je vyhledávat pro budoucí AI chat.

**Architecture:** Integration vlastní OpenSearch SDK, integrační model a repository. Application mapuje modely a validuje vstupy. Host vlastní DI a konfiguraci.

**Tech Stack:** .NET 10, OpenSearch.Client 2.2.0, FluentValidation, AutoMapper.

**Spec:** ../specs/2026-11-23-opensearch-documents-design.md

## Global Constraints

- Bez testů a testovacích závislostí podle AGENTS.md; ověření buildem a kontrolou kódu.
- Bez endpointů, změn migrací a zásahů do uživatelských změn.
- Bez CancellationToken na vlastních rozhraních; options bez C# výchozích hodnot.
- Práce v aktuálním workspace, bez přesunu rozpracovaných změn a bez commitů.

## Review Focus

- Prázdné ID/title/body a neplatné URL nevolají repository.
- Prázdný dotaz a limit mimo 1–100 nevolají repository.
- Chybějící index vrátí prázdné výsledky, souběžné vytvoření nevadí.
- Neúspěšný či neúplný výsledek OpenSearch se nepovažuje za úspěch.
- Hesla, obsah dokumentů a SDK debug informace nejsou ve výjimkách.

## Task 1: Integrace

- [x] Přidat OpenSearch.Client do Integration, SearchDocument do IntegrationModels a IDocumentSearchRepository do Repositories/Interfaces.
- [x] Implementovat IndexAsync(SearchDocument) a SearchAsync(string searchText, int limit) v DocumentSearchRepository, včetně privátní inicializace indexu.
- [x] Přidat IOpenSearchOptions a OpenSearchOptions v Core/Configurations.

## Task 2: Aplikační logika

- [x] Přidat Models/Documents/Document s Guid Id, Title, Body, Url.
- [x] Přidat DocumentValidator, mapování Document ↔ SearchDocument a IDocumentService/DocumentService.
- [x] UpsertAsync(Document) vrací ApplicationResult<NoValueResult>; SearchAsync(string, int limit = 5) vrací ApplicationResult<IReadOnlyList<Document>>.

## Task 3: Zapojení a ověření

- [x] Přidat OpenSearchServiceCollectionExtension s AddOpenSearchRegistration, singleton SDK klientem, kontrolou HTTPS a přihlašovacích údajů.
- [x] Doplnit stávající DI, Program a OpenSearchOptions v appsettings pouze cílenými úpravami.
- [x] Přidat docs/opensearch.md s konfigurací, významem ID, eventual consistency a hranicemi integrace.
- [x] Spustit dotnet build PublicSafetyHub.Host/PublicSafetyHub.Host.csproj a git diff --check; zkontrolovat změny proti původnímu stavu.

## Výsledek

- `dotnet build PublicSafetyHub.Host/PublicSafetyHub.Host.csproj --nologo -p:StartDevelopmentDatabase=false` skončil úspěšně, 0 chyb. Vlastní nové upozornění IDE0305 bylo opraveno; poslední inkrementální build měl 0 upozornění. Při prvním sestavení se objevila existující stylová upozornění v uživatelské migraci, která se neměnila.
- `git diff --check` bez závad. Statická nezávislá revize bez nálezů.
- Ruling: původní build spouštěl Docker Desktop, kde narazil na oprávnění. Ověření používá existující přepínač StartDevelopmentDatabase=false; konfigurace projektu se kvůli tomu neměnila.
- Ruling: chybějící index je rozpoznán konkrétní chybou index_not_found_exception. Ostatní nedostupné či zavřené indexy se nepovažují za prázdné výsledky.
- Testy se podle zadání nepsaly ani nespouštěly. Živé ověření Aivenu vyžaduje připojení a přihlašovací údaje.
