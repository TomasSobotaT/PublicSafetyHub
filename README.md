
# PublicSafetyHub

Vývojová PostgreSQL databáze se spouští při Debug buildu projektu Host přes Docker Compose. Docker Desktop musí být nainstalovaný.

Připojení k běžící databázi:

| Nastavení | Hodnota |
| --- | --- |
| Server | localhost |
| Port | 5433 |
| Databáze | PublicSafetyHub |
| Uživatel | publicsafetyhub |
| Heslo | publicsafetyhub_dev |

Ve Visual Studiu lze použít integrovaný terminál (View → Terminal):

```powershell
docker exec -it publicsafetyhub-db psql -U publicsafetyhub -d PublicSafetyHub
```

Příkaz `\dt` zobrazí tabulky, `\q` ukončí psql.

Databáze používá dočasné úložiště tmpfs. Při každém startu API v prostředí Development se zapnutým `DevelopmentDatabase:Enabled` se databáze smaže a vytvoří znovu podle aktuálního EF modelu. Ztratí se všichni uživatelé i články. Samotný build data nemaže; zastavení databázového kontejneru jeho dočasná data odstraní. V produkci je `DevelopmentDatabase:Enabled` ve výchozím stavu vypnuté.
