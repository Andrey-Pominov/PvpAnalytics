# PvpAnalytics

PvP combat analytics for World of Warcraft combat logs. Upload a `WoWCombatLog*.txt` file to parse arena matches, automatically persist players, matches, results, and granular combat log entries into PostgreSQL, and query them via a simple REST API.

## Contents
- Architecture
- Data model
- API overview
- Run locally (dotnet)
- Run with Docker Compose
- Configuration
- Log upload and parsing details
- Development notes

## Architecture

Solution layout (multi-project .NET 9):

- `PvpAnalytics.Api` (ASP.NET Core): Web API, OpenAPI, controllers (CRUD + log upload)
- `PvpAnalytics.Application`: Services and log parser/ingestion pipeline
- `PvpAnalytics.Infrastructure`: EF Core, DbContext, migrations, repository implementation
- `PvpAnalytics.Core`: Domain entities, enums, and parsing constants/models
- `PvpAnalytics.Shared`, `PvpAnalytics.Worker`, `PvpAnalytics.Tests`: reserved for shared code, background jobs, and tests

Key patterns:
- EF Core with PostgreSQL
- Generic repository with optional unit-of-work (`autoSave` flag)
- CRUD services per entity (`ICrudService<T>`) used by API controllers
- Language-agnostic combat log parser (uses numeric IDs and field mappings)

## Data model

Entities (in `PvpAnalytics.Core/Entities`):
- `Player` (Name, Realm, Class, Spec, Faction)
- `Match` (UniqueHash, CreatedOn, MapName, GameMode, Duration, IsRanked)
- `MatchResult` (MatchId, PlayerId, Team, RatingBefore/After, IsWinner)
- `CombatLogEntry` (MatchId, Timestamp, SourcePlayerId, TargetPlayerId, Ability, DamageDone, HealingDone, CrowdControl)

Indexes/invariants:
- `Match.UniqueHash` unique index
- `MatchResult (MatchId, PlayerId)` unique index
- `CombatLogEntry` uses restricted deletes for Source/Target players

## API overview

Base path: `/api`

- `GET /api/health` – basic status
- `GET /api/players`, `GET /api/players/{id}`, `POST`, `PUT`, `DELETE`
- `GET /api/matches`, `GET /api/matches/{id}`, `POST`, `PUT`, `DELETE`
- `GET /api/matchresults`, `GET /api/matchresults/{id}`, `POST`, `PUT`, `DELETE`
- `GET /api/combatlogentries`, `GET /api/combatlogentries/{id}`, `POST`, `PUT`, `DELETE`
- `POST /api/logs/upload` – upload a WoW combat log file (`multipart/form-data`, field `file`)
  - Responds 201 Created with `Location: /api/matches/{id}` when a match was persisted
  - Responds 202 Accepted when no match could be inferred from the file

OpenAPI (Swagger) is enabled in Development: browse to `/openapi/v1.json` (UI mapping is present in dev).

## Run locally (dotnet)

Prereqs: .NET 9 SDK, PostgreSQL 16+

1) Set config in `PvpAnalytics.Api/appsettings.json` (or environment variables):
```
ConnectionStrings:DefaultConnection = Host=localhost;Port=5432;Database=pvpdb;Username=pvp;Password=pvp123
```
2) Run EF migrations automatically at startup (already configured):
   - The API calls `db.Database.Migrate()` on start
3) Start the API:
```
dotnet run --project PvpAnalytics.Api
```
4) Test health:
```
GET http://localhost:5000/api/health
```

## Run with Docker Compose

Compose file: `compose.yaml`

- Services:
  - `db` (Postgres 16) – exposed on host port 5433
  - `pvpanalytics` (ASP.NET Core API) – exposed on host port 8080

Commands:
```
docker compose build
docker compose up -d
```
API: `http://localhost:8080`

Environment (in compose):
- `ConnectionStrings__DefaultConnection=Host=db;Port=5432;Username=pvp;Password=pvp123;Database=pvpdb`
- `ASPNETCORE_URLS=http://+:8080`

Migrations run at container start (automatic `Database.Migrate()`).

## Log upload and parsing details

Endpoint: `POST /api/logs/upload` with `multipart/form-data` (key `file`)

Parser features (`PvpAnalytics.Application/Logs`):
- Language-agnostic: relies on numeric IDs and structured fields
- Uses `CombatLogFieldMappings` and `CombatLogEventTypes`
- Timestamps: treated as local time and normalized to UTC (`AssumeLocal | AdjustToUniversal`)
- Absorbed amounts: parsed from `SpellAbsorbed.Amount` (null if missing/unparseable)
- Player resolution: normalize `Name-Realm` to `Name`, cache hits avoid repeated DB calls
- Entries are only persisted when a valid `SourcePlayerId` exists (no FK=0)

Match detection (`CombatLogIngestionService`):
- Detects arena sessions using `ZONE_CHANGE` with known zone IDs (`ArenaZoneIds`)
- Buffers entries and participants during an arena session
- Finalizes a `Match` and `MatchResult`s on zone switch or EOF
- `GameMode` derived from participant count via `GameModeHelper`:
  - 4 → TwoVsTwo
  - 6 → ThreeVsThree
  - 10 → Skirmish (fallback; enum lacks 5v5)
  - otherwise → TwoVsTwo (default)

## Configuration

- Connection string: `ConnectionStrings:DefaultConnection` (or env var `ConnectionStrings__DefaultConnection`)
- ASP.NET Core: `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`

## Development notes

- Generic repository (`IRepository<TEntity>`) with optional `autoSave` parameter on `Add/Update/Delete` for unit-of-work batching
- CRUD services implement `ICrudService<TEntity>` and are registered in `AddApplication()`
- API controllers are minimal and operate directly on entities (DTOs can be added later)
- Health endpoint: `GET /api/health`
- Postman collection example is provided in earlier conversation; generate via OpenAPI if needed

## License

MIT (or your preferred license)
