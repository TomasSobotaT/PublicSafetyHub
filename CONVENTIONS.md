

- Při `Nullable=disable` neinicializuj referenční vlastnosti jen kvůli nullability, např. nepoužívej `= string.Empty` ani `= null!`. Inicializace kolekcí potřebné pro jejich použití a skutečné provozní výchozí hodnoty zůstávají.
