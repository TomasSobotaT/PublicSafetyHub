# OpenSearch dokumenty pro AI chat

Dokumenty jsou nezávislé na článcích a ukládají se pouze do vlastního OpenSearch indexu. PostgreSQL, import článků a jejich embeddingy se nemění. Zatím nejsou implementované žádné endpointy pro dokumenty ani AI chat. Budoucí upload controller může volat `IDocumentService.SaveDocumentAsync`; interní hledání může používat `IDocumentService.SearchDocumentsAsync`.

## Konfigurace Aivenu

`PublicSafetyHub.Host/appsettings.json` obsahuje sekci `OpenSearchOptions` a název indexu `chat-documents`. Nastav HTTPS adresu služby OpenSearch včetně portu, nikoli adresu Dashboards. URL nesmí obsahovat přihlašovací údaje, cestu ani query. Klient standardně ověřuje TLS certifikát.

Pro lokální vývoj nastav hodnoty ve složce solution:

```powershell
dotnet user-secrets set --project PublicSafetyHub.Host "OpenSearchOptions:Url" "https://YOUR-SERVICE.aivencloud.com:12345"
dotnet user-secrets set --project PublicSafetyHub.Host "OpenSearchOptions:Username" "YOUR-USERNAME"
dotnet user-secrets set --project PublicSafetyHub.Host "OpenSearchOptions:Password" "YOUR-PASSWORD"
```

Pro nasazení použij například `OpenSearchOptions__Url`, `OpenSearchOptions__Username`, `OpenSearchOptions__Password` a případně `OpenSearchOptions__DocumentsIndex`. Hesla ani Service URI s heslem neukládej do Gitu. Index musí mít malý název tvořený ASCII písmeny, číslicemi, pomlčkou či podtržítkem, začínat písmenem nebo číslicí a mít nejvýše 255 znaků.

Klient je singleton vytvořený při prvním použití integrace. Prázdné připojení neblokuje start zbytku API; první použití neúplné či neplatné konfigurace vyvolá výjimku. Uživatel OpenSearch potřebuje oprávnění číst dokumenty, ověřit existenci indexu, vytvořit index a zapisovat dokumenty.

## Použití aplikační služby

```csharp
var document = new Document
{
    Title = "Postup při mimořádné události",
    Body = "Celý text dokumentu…",
    Url = "https://example.cz/postup"
};

var saved = await documentService.SaveDocumentAsync(document);
if (saved.IsSuccess)
{
    document.Id = saved.Value;
}

var found = await documentService.SearchDocumentsAsync("mimořádná událost", limit: 5);
var semantic = await documentService.SearchDocumentsAsync("Jak postupovat při nehodě?", searchMode: SearchMode.Semantic);
var hybrid = await documentService.SearchDocumentsAsync("mimořádná událost", searchMode: SearchMode.Hybrid);
```

`Document` je v `PublicSafetyHub.Models.Documents`, `SearchMode` v `PublicSafetyHub.Core.Enums`, služba je dostupná přes `PublicSafetyHub.Application.Services.Interfaces.IDocumentService`. Obě operace vracejí `ApplicationResult`: před čtením `Value` ověř `IsSuccess`; validační chyby jsou v `Error`. Technická selhání OpenSearch vyvolávají výjimku pro stávající exception handling.

- `Id`: pro nový dokument ponech `Guid.Empty`; služba po validaci obsahu vygeneruje UUID v7 přes `Guid.CreateVersion7()`. Úspěšný výsledek `SaveDocumentAsync` vrací ID uloženého dokumentu. Vstupní objekt se nemění, proto si ID z výsledku uchovej pro další aktualizace. UUID v7 obsahuje UTC časovou značku svého vytvoření; při aktualizaci se ID ani tato značka nemění. Existující ID služba zachová a celý dokument nahradí, včetně URL. Při opakování požadavku bez ID vzniká nový dokument; pokud potřebuješ zachovat ID i při nejistém výsledku předchozího zápisu, přiděl je před prvním voláním. Stejné URL s jiným ID je jiný dokument.
- `Title`: povinný neprázdný titulek, nejvýše 512 znaků.
- `Body`: povinný neprázdný text.
- `Url`: volitelné absolutní HTTP(S) URL bez přihlašovacích údajů, nejvýše 2048 znaků. URL se ukládá pro citaci zdroje, neprohledává se.

Při každém uložení Application zavolá stávající `IEmbeddingService` s textem `Title: {Title}\n\n{Body}`. URL se do embeddingu nezahrnuje. Titulek a tělo se před sestavením textu oříznou o krajní mezery; uložený původní obsah se nemění. Vstup nad 8191 tokenů se odmítne validační chybou bez indexace a bez volání generování embeddingu. Požadovaný rozměr je 1536 a je společný přes `EmbeddingConstants.Dimensions`. SDK vrácené vektory kontroluje na rozměr a konečné hodnoty; nulové vektory se odmítají. Při každé aktualizaci se embedding přepočítá, aby odpovídal uloženému textu.

Vektor je uložený v `IntegrationModels.SearchDocument.Embedding` jako `float[]` s výchozí hodnotou `[]`. Volající předává pouze `Document`; embedding se nepřebírá od klienta a nevrací se ve výsledném modelu `Document`. Vyhledávání pole `embedding` vynechává už při čtení `_source` z OpenSearch.

Pro uložení, sémantické a hybridní hledání nastav také existující `OpenAiOptions:ApiKey` a `OpenAiOptions:EmbeddingModel` (výchozí `text-embedding-3-small`). Používá se stejný `IEmbeddingService` jako u článků, avšak úložiště dokumentů zůstává oddělené. Chybějící konfigurace vrací `ResultHelper.Unavailable`, před zápisem se nic neuloží.

První zápis vytvoří index s explicitním striktním mappingem: `id` jako keyword, `title` a `body` jako text s vestavěným českým analyzérem, `url` jako neindexovaný keyword a `embedding` jako `knn_vector` s rozměrem 1536. Do existujícího indexu se při ukládání přidá mapping pole `embedding` přes Put Mapping; účet potřebuje i toto oprávnění. Existující nekompatibilní typ nebo rozměr pole se automaticky nepřevádí. Změna analyzéru později vyžaduje nový index a přeindexování dokumentů. Inicializace toleruje souběžné vytvoření stejného indexu jiným požadavkem.

Starší dokumenty bez vektoru se dál najdou fulltextem, ale sémantika je vynechá. Je potřeba je znovu uložit pod původním ID. Automatický backfill se nespouští. Po změně embedding modelu nebo přípravy jeho vstupu je nutné přegenerovat všechny dokumenty společně; nemíchej v jednom indexu vektory z různých modelů.

Hledání má režimy:

- `FullText` (výchozí): `multi_match` přes titulek (váha 2) a tělo (váha 1). Nevolá generování embeddingu ani nevyžaduje OpenAI klíč.
- `Semantic`: Application vygeneruje embedding dotazu a repository použije OpenSearch `script_score` s `knn_score`, jazykem `knn` a prostorem `cosinesimil`. Dotaz nejprve vybere jen dokumenty, které embedding mají.
- `Hybrid`: provede oba způsoby nad nejvýše 100 kandidáty z každé větve, sjednotí dokumenty podle ID a pořadí spojí pomocí RRF s konstantou 60. Nakonec vrátí požadovaný počet výsledků. Nesčítá přímo nesrovnatelná skóre fulltextu a podobnosti vektorů.

Použité přesné vektorové hledání prochází všechny vyhovující vektory. Nevyžaduje `index.knn=true`, HNSW index ani serverovou search pipeline, potřebuje však OpenSearch k-NN plugin s podporou skriptu `knn_score`. Pro velké objemy dokumentů zvaž přechod na přibližné ANN hledání s odpovídajícím indexem. Viz [oficiální dokumentace přesného k-NN](https://docs.opensearch.org/latest/vector-search/vector-search-techniques/knn-score-script/).

Dotaz musí být neprázdný a mít nejvýše 2048 znaků; pro sémantiku se ověřuje i limit tokenů embedding modelu. Limit výsledků je 1–100, výchozí 5. Chybějící index před prvním zápisem vrací prázdný seznam. Timeout či selhání shardů se nepovažují za platný prázdný výsledek a hybrid při selhání některé větve nevrací pouze druhou větev.

Zápis nevyžaduje okamžitý refresh indexu, takže se dokument v hledání objeví až po běžném refreshi OpenSearch. Nejde o EF repository a zápis nepotřebuje `IUnitOfWork.SaveChangesAsync`. Neprovádí se synchronizace s PostgreSQL, čtení souborů/PDF, dělení textu ani mazání dokumentů.

## Ověření

Bez připojení k Aivenu ověř sestavení a statickou kontrolu změn:

```powershell
dotnet build PublicSafetyHub.Host/PublicSafetyHub.Host.csproj -p:StartDevelopmentDatabase=false
git diff --check
```

Pro živé ověření je potřeba nastavit připojení a ze skutečného aplikačního volajícího zapsat dokument, po refreshi jej vyhledat a přepsat pod stejným ID. Testovací endpointy nejsou součástí integrace.

SDK a kompatibilita: [OpenSearch.Client návod](https://docs.opensearch.org/latest/clients/OSC-dot-net/), [.NET klienti](https://docs.opensearch.org/latest/clients/dot-net/).
