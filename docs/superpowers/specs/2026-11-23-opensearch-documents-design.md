# Samostatné dokumenty pro AI chat

Schválený rozsah: dokumenty jsou nezávislé na článcích. Integration obsahuje OpenSearch klienta prostřednictvím SDK, integrační model a repository pro zápis/aktualizaci a interní hledání. Application obsahuje validaci a službu. Host registruje konfiguraci a DI. Žádné endpointy ani testy.

## Přístup

Použijeme samostatný index a dokument s Guid Id, Title, Body a Url. Opakovaný zápis stejného ID nahradí celý dokument. Fulltext hledá Title a Body, titulek má dvojnásobnou váhu. Jde o fulltext, bez embeddingů. Vlastní index zabrání vazbě na PostgreSQL články; použití indexu článků by odporovalo potvrzenému oddělení.

Implementační volby: index při prvním zápisu vytvoří repository s explicitním mappingem a českým analyzérem. Souběžné vytvoření stejného indexu se toleruje. Hledání před prvním zápisem vrátí prázdný seznam. Výchozí limit hledání je 5, povolený rozsah 1–100. Vyžadují se neprázdné ID, titulek a tělo; URL je volitelné absolutní HTTP(S). Klient je singleton, ostatní služby scoped. Připojení používá HTTPS a standardní ověřování certifikátů. Přihlašovací údaje nepatří do Gitu. Chybějící konfigurace se projeví při prvním použití klienta, nikoli při startu nepoužívané integrace.

Stávající články, migrace a necommitované změny uživatele se zachovají. Změny se ověří sestavením Host a kontrolou diffu; bez připojení k Aivenu nelze potvrdit živou indexaci. AI chat ani jeho nástroje tento krok nevytváří.
