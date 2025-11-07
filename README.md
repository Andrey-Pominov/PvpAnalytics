# PvpAnalytics

PvP combat analytics for World of Warcraft combat logs. Upload a `WoWCombatLog*.txt` file to parse arena matches, automatically persist players, matches, results, and granular combat log entries into PostgreSQL, and query them via a simple REST API.

## Contents
- Architecture
- Auth microservice
- Data model
- API overview
- Run locally (dotnet)
- Run with Docker Compose
- Configuration
- Log upload and parsing details
- Authentication workflow
- Development notes

## Architecture

Solution layout (multi-project .NET 9):

- `PvpAnalytics.Api` (ASP.NET Core): Web API, OpenAPI, controllers (CRUD + log upload)
- `PvpAnalytics.Application`: Services and log parser/ingestion pipeline
- `PvpAnalytics.Infrastructure`: EF Core, DbContext, migrations, repository implementation
- `PvpAnalytics.Core`: Domain entities, enums, and parsing constants/models
- `AuthService.Api`: ASP.NET Core Identity microservice exposing register/login/refresh endpoints
- `AuthService.Infrastructure`: Identity EF Core context, Identity store configuration, services
- `AuthService.Application`: DTOs and abstractions for auth flows
- `AuthService.Core`: Shared auth domain primitives (e.g., refresh tokens)
- `PvpAnalytics.Shared`, `PvpAnalytics.Worker`, `PvpAnalytics.Tests`: reserved for shared code, background jobs, and tests

Key patterns:
- EF Core with PostgreSQL
- Generic repository with optional unit-of-work (`autoSave` flag)
- CRUD services per entity (`ICrudService<T>`) used by API controllers
- Language-agnostic combat log parser (uses numeric IDs and field mappings)

## Auth microservice

The `AuthService` projects provide isolated authentication/authorization concerns:

- ASP.NET Core Identity + EF Core (PostgreSQL) for user management and refresh-token storage.
- Automatic migrations on startup (`AuthDbContext.Database.Migrate()`).
- JWT issuance with configurable issuer/audience/signing key.

### Endpoints (base URL: `http://localhost:8081/api/auth`)

| Method | Path            | Description                |
|--------|-----------------|----------------------------|
| POST   | `/register`     | Create a user (email + password + optional full name). |
| POST   | `/login`        | Validate credentials and return tokens. |
| POST   | `/refresh`      | Exchange a refresh token for a new access token. |

Each endpoint returns an `AuthResponse` payload:

```json
{
  "accessToken": "{JWT}",
  "accessTokenExpiresAt": "2025-11-07T12:34:56Z",
  "refreshToken": "{opaque}",
  "refreshTokenExpiresAt": "2025-11-14T12:34:56Z"
}
```

Errors are returned as `400 Bad Request` with `{ "error": "message" }`.

Configuration keys used by the service:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=authdb;Username=auth;Password=auth123"
},
"Jwt": {
  "Issuer": "PvpAnalytics.Auth",
  "Audience": "PvpAnalytics.Api",
  "SigningKey": "CHANGE_ME",
  "AccessTokenMinutes": 60,
  "RefreshTokenDays": 7
}
```

All `register/login/refresh` routes are `[AllowAnonymous]`; any future authenticated endpoints can rely on `[Authorize]`.

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

- All controllers (except `GET /api/health`) are protected with `[Authorize]`; include `Authorization: Bearer <access_token>` obtained from the auth service.

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

### AuthService.Api

1. Update `AuthService.Api/appsettings.Development.json` (or environment variables) with database + JWT details.
2. Start the auth service:
   ```bash
   dotnet run --project AuthService.Api --urls http://localhost:8081
   ```
3. Register/login:
   ```bash
   curl -X POST http://localhost:8081/api/auth/register \
        -H "Content-Type: application/json" \
        -d '{"email":"user@example.com","password":"Passw0rd!","fullName":"Test User"}'

   curl -X POST http://localhost:8081/api/auth/login \
        -H "Content-Type: application/json" \
        -d '{"email":"user@example.com","password":"Passw0rd!"}'
   ```
   Save the `accessToken` and `refreshToken` values.

### PvpAnalytics.Api

1. Configure `PvpAnalytics.Api/appsettings.Development.json` with the analytics DB connection string and matching JWT settings (issuer/audience/key must mirror the auth service).
2. Start the analytics API:
   ```bash
   dotnet run --project PvpAnalytics.Api --urls http://localhost:8080
   ```
3. Health check (unauthenticated):
   ```bash
   curl http://localhost:8080/api/health
   ```
4. Authenticated request example:
   ```bash
   curl http://localhost:5000/api/players \
        -H "Authorization: Bearer <access_token>"
   ```

## Run with Docker Compose

Compose file: `compose.yaml`

- Services:
  - `db` (Postgres 16) – exposed on host port 5433
  - `pvpanalytics` (analytics API) – exposed on host port 8080
  - `auth` (auth microservice) – exposed on host port 8081

Commands:
```
docker compose build
docker compose up -d
```
APIs:
- Auth: `http://localhost:8081`
- Analytics: `http://localhost:8080`

Environment (in compose):
- `ConnectionStrings__DefaultConnection=Host=db;Port=5432;Username=pvp;Password=pvp123;Database=pvpdb`
- `ASPNETCORE_URLS=http://+:8080`
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` (shared between auth + analytics)

The Postgres container mounts `docker/postgres/init.sql` to create the `auth` user and `authdb` database on first run.

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

## Authentication workflow

1. Register or login against the auth service (`/api/auth/register` or `/api/auth/login`) to obtain `accessToken` and `refreshToken` values.
2. Call analytics endpoints with the access token:
   ```bash
   curl http://localhost:8080/api/matches \
        -H "Authorization: Bearer <access_token>"
   ```
3. When the access token expires, exchange the refresh token:
   ```bash
   curl -X POST http://localhost:8081/api/auth/refresh \
        -H "Content-Type: application/json" \
        -d '{"refreshToken":"<refresh_token>"}'
   ```
4. Refresh tokens are single-use: the previous token is revoked as soon as it is exchanged.

## Configuration

- Connection strings:
  - Analytics API: `ConnectionStrings:DefaultConnection` (`ConnectionStrings__DefaultConnection`)
  - Auth service: same key pointing at `authdb`
- JWT options (`Jwt` section / `Jwt__*` environment variables) must match between services:
  - `Issuer`, `Audience`, `SigningKey`, `AccessTokenMinutes`, `RefreshTokenDays`
- ASP.NET Core host settings: `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`

## Development notes

- Generic repository (`IRepository<TEntity>`) with optional `autoSave` parameter on `Add/Update/Delete` for unit-of-work batching
- CRUD services implement `ICrudService<TEntity>` and are registered in `AddApplication()`
- API controllers are minimal and operate directly on entities (DTOs can be added later)
- Health endpoint: `GET /api/health`
- AuthService uses ASP.NET Core Identity with JWT + refresh tokens; extend via `IdentityService` if additional policies/claims are needed
- Postman collection example is provided in earlier conversation; generate via OpenAPI if needed

## License

MIT (or your preferred license)
