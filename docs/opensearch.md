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
```

`Document` je v `PublicSafetyHub.Models.Documents`, služba je dostupná přes `PublicSafetyHub.Application.Services.Interfaces.IDocumentService`. Obě operace vracejí `ApplicationResult`: před čtením `Value` ověř `IsSuccess`; validační chyby jsou v `Error`. Technická selhání OpenSearch vyvolávají výjimku pro stávající exception handling.

- `Id`: pro nový dokument ponech `Guid.Empty`; služba po validaci obsahu vygeneruje UUID v7 přes `Guid.CreateVersion7()`. Úspěšný výsledek `SaveDocumentAsync` vrací ID uloženého dokumentu. Vstupní objekt se nemění, proto si ID z výsledku uchovej pro další aktualizace. UUID v7 obsahuje UTC časovou značku svého vytvoření; při aktualizaci se ID ani tato značka nemění. Existující ID služba zachová a celý dokument nahradí, včetně URL. Při opakování požadavku bez ID vzniká nový dokument; pokud potřebuješ zachovat ID i při nejistém výsledku předchozího zápisu, přiděl je před prvním voláním. Stejné URL s jiným ID je jiný dokument.
- `Title`: povinný neprázdný titulek, nejvýše 512 znaků.
- `Body`: povinný neprázdný text.
- `Url`: volitelné absolutní HTTP(S) URL bez přihlašovacích údajů, nejvýše 2048 znaků. URL se ukládá pro citaci zdroje, neprohledává se.

První zápis vytvoří index s explicitním striktním mappingem: `id` jako keyword, `title` a `body` jako text s vestavěným českým analyzérem, `url` jako neindexovaný keyword. Existující index se automaticky nemigruje; při ručním vytvoření zajisti stejný mapping. Změna analyzéru později vyžaduje nový index a přeindexování dokumentů. Inicializace toleruje souběžné vytvoření stejného indexu jiným požadavkem.

Hledání používá fulltext `multi_match` přes titulek (váha 2) a tělo (váha 1), vrací nejrelevantnější dokumenty. Nejde o sémantické hledání ani RAG pipeline. Dotaz musí být neprázdný a mít nejvýše 2048 znaků; limit je 1–100, výchozí 5. Chybějící index před prvním zápisem vrací prázdný seznam. Timeout či selhání shardů se nepovažují za platný prázdný výsledek.

Zápis nevyžaduje okamžitý refresh indexu, takže se dokument v hledání objeví až po běžném refreshi OpenSearch. Nejde o EF repository a zápis nepotřebuje `IUnitOfWork.SaveChangesAsync`. Neprovádí se synchronizace s PostgreSQL, čtení souborů/PDF, dělení textu, generování embeddingů ani mazání dokumentů.

## Ověření

Bez připojení k Aivenu ověř sestavení a statickou kontrolu změn:

```powershell
dotnet build PublicSafetyHub.Host/PublicSafetyHub.Host.csproj -p:StartDevelopmentDatabase=false
git diff --check
```

Pro živé ověření je potřeba nastavit připojení a ze skutečného aplikačního volajícího zapsat dokument, po refreshi jej vyhledat a přepsat pod stejným ID. Testovací endpointy nejsou součástí integrace.

SDK a kompatibilita: [OpenSearch.Client návod](https://docs.opensearch.org/latest/clients/OSC-dot-net/), [.NET klienti](https://docs.opensearch.org/latest/clients/dot-net/).
