# Bitcoin Tracker (.NET)

Aplikace pro sledovani kurzu Bitcoinu postavena na C# a ASP.NET Core MVC.

## Funkce

- Ziva data: Aktualni kurz Bitcoinu z CoinDesk (BTC/EUR) prepocteny na BTC/CZK pomoci kurzu CNB
- Sprava dat: Ulozeni aktualniho kurzu do databaze, editace poznamek, mazani zaznamu
- Graf: Zobrazeni trendu ulozenych kurzu pomoci Chart.js
- Tabulka: Razeni, filtrovani a strankovani pomoci DataTables (cestina)
- Validace: Kontrola poznamek pred ulozenim
- Odolnost: Retry politika pomoci Polly pro external API volani
- Logovani: Strukturovane logovani pres Serilog (konzole + soubor)
- Testy: Unit testy v xUnit a Moq s pokrytim nad 95 %
- XSS ochrana: Veskery uzivatelsky obsah se vykresluje pres textContent API
- Healthcheck: Docker Compose obsahuje healthcheck pro SQL Server

## Technologie

- Backend: C# / .NET 8.0 (ASP.NET Core MVC), Entity Framework Core
- Frontend: Bootstrap 5, jQuery, DataTables, Chart.js
- Databaze: MS SQL Server (Docker/Production) nebo SQLite (local development)
- Logovani: Serilog (Console + File)
- Odolnost: Polly (HTTP retry policies)
- Testy: xUnit, Moq, Microsoft.EntityFrameworkCore.InMemory, coverlet

## Zacinate

### Predpoklady

- Pro Docker: Docker a Docker Compose
- Pro lokalni vyvoj: .NET SDK 8.0 (SQL Server neni potreba)

### Moznost 1: Docker (Produkce - SQL Server)

```
docker-compose up --build
```

Otevri prohlizec na http://localhost:5000

### Moznost 2: Lokalni vyvoj (bez Dockeru, bez SQL Serveru)

Aplikace automaticky pouzije SQLite v Development modu:

```
dotnet run
```

Nebo s explicitni URL:

```
dotnet run --urls "http://localhost:5295"
```

Otevri prohlizec na http://localhost:5295

Kdyz je ASPNETCORE_ENVIRONMENT=Development, aplikace pouzije SQLite (BitcoinTracker.db). V Production/Docker rezimu pouzije SQL Server.

## Architektura

### Databazova strategie

- Development (dotnet run): SQLite, bez dalsich zavislosti
- Docker (docker-compose up): SQL Server, nastaveni pres environment variable
- Production: SQL Server, vyzaduje connection string

### Struktura projektu

- Controllers/: API a MVC controllery
- Models/: EF Core entita a DbContext
- Services/: Business logika (BitcoinService, DatabaseInitializer)
- Views/: Razor sablony
- wwwroot/: Staticke soubory (CSS, JS, lib)

## Spusteni testu

### Lokalni testy

```
dotnet test
```

S pokrytim kodu:

```
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
```

### Docker testy

```
chmod +x run-tests-in-docker.sh
./run-tests-in-docker.sh
```

## API dokumentace

Base URL: http://localhost:5000/api

### Endpointy

| Metoda | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/live | Ziska aktualni BTC/EUR a BTC/CZK kurz |
| POST | /api/live | Ulozi aktualni kurz do databaze |
| GET | /api/rates | Vrati vsechny ulozene kurzy |
| PATCH | /api/rates/{id} | Upravi poznamku u ulozeneho kurzu |
| DELETE | /api/rates/{id} | Smaze jeden zaznam |

### Priklady pouziti

Ziskani ziveho kurzu:
```
curl http://localhost:5000/api/live
```

Ulozeni aktualniho kurzu:
```
curl -X POST http://localhost:5000/api/live
```

Seznam ulozenych kurzu:
```
curl http://localhost:5000/api/rates
```

Uprava poznamky:
```
curl -X PATCH http://localhost:5000/api/rates/1 \
  -H "Content-Type: application/json" \
  -d '{"note": "Nakup za tuto cenu"}'
```

Smazani zaznamu:
```
curl -X DELETE http://localhost:5000/api/rates/1
```

## Konfigurace

### Environment promenne

| Promenna | Ucel | Vychozi |
|----------|------|---------|
| ASPNETCORE_ENVIRONMENT | Detection Development/Production | Development |
| ConnectionStrings__DefaultConnection | SQL Server connection string | Povinny v produkci |

### Connection string pro Docker

V docker-compose.yml je nastaven automaticky:

```
ConnectionStrings__DefaultConnection=Server=db;Database=BitcoinTracker;User Id=sa;Password=YourStrong!Password;TrustServerCertificate=True
```

### Logovani

Serilog uklada konzole (informacni zpravy) a soubor s denni rotaci (7 dni archiv). Logy jsou v adresari logs/.

## Databaze

- SQL Server (Docker): Pouziva scripts/setup.sql
- SQLite (Lokalne): Auto-vytvorena jako BitcoinTracker.db (gitignored)

## Dulezite detaily implementace

- CoinDesk API: Parsuje Data.BTC-EUR.PRICE z JSON odpovedi
- CNB API: JSON endpoint na api.cnb.cz
- XSS ochrana: Frontend pouziva textContent pro veskery uzivatelsky obsah
- Chybove stavy: Pri selhani API aplikace stale funguje (vrati null pro chybejici data)
- Bez hardcode fallbacku: Pouze realna trzni data
