# PvpAnalytics

**Author:** Rmpriest

PvP combat analytics for World of Warcraft combat logs. Upload a `WoWCombatLog*.txt` file to parse arena matches, automatically persist players, matches, results, and granular combat log entries into PostgreSQL, and query them via a simple REST API.

## Table of Contents

- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Data Model](#data-model)
- [Authentication](#authentication)
- [API Reference](#api-reference)
- [Setup & Configuration](#setup--configuration)
- [Log Upload & Parsing](#log-upload--parsing)
- [UI Frontend](#ui-frontend)
- [Testing](#testing)
- [Development Notes](#development-notes)
- [License](#license)

## Quick Start

### Docker Compose (Recommended)

1. Create a `.env` file in the project root:
   ```bash
   SA_PASSWORD=[YOUR_SA_PASSWORD]
   JWT_SIGNING_KEY=[YOUR_JWT_SIGNING_KEY]
   ```

2. Start all services:
   ```bash
   docker compose build
   docker compose up -d
   ```

3. Access the services:
   - Auth API: `http://localhost:8081`
   - Analytics API: `http://localhost:8080`

### Local Development

**Prerequisites:** .NET 9 SDK, PostgreSQL 16+, SQL Server 2022+

See [Setup & Configuration](#setup--configuration) for detailed setup instructions.

## Architecture

### Solution Structure

Multi-project .NET 9 solution:

**PvpAnalytics Projects:**
- `PvpAnalytics.Api` (ASP.NET Core): Web API, OpenAPI, controllers (CRUD + log upload)
- `PvpAnalytics.Application`: Services and log parser/ingestion pipeline
- `PvpAnalytics.Infrastructure`: EF Core, DbContext, migrations, repository implementation
- `PvpAnalytics.Core`: Domain entities, enums, and parsing constants/models
- `PvpAnalytics.Shared`, `PvpAnalytics.Worker`, `PvpAnalytics.Tests`: Reserved for shared code, background jobs, and tests

**AuthService Projects:**
- `AuthService.Api`: ASP.NET Core Identity microservice exposing register/login/refresh endpoints
- `AuthService.Infrastructure`: Identity EF Core context, Identity store configuration, services
- `AuthService.Application`: DTOs and abstractions for auth flows
- `AuthService.Core`: Shared auth domain primitives (e.g., refresh tokens)

### Key Patterns

- **EF Core with PostgreSQL** for analytics data storage
- **SQL Server** for authentication data storage
- **Generic repository** with optional unit-of-work (`autoSave` flag)
- **CRUD services** per entity (`ICrudService<T>`) used by API controllers
- **Language-agnostic combat log parser** (uses numeric IDs and field mappings)

## Data Model

### Core Entities

Located in `PvpAnalytics.Core/Entities`:

- **Player**: Name, Realm, Class, Spec, Faction
- **Match**: UniqueHash, CreatedOn, MapName, GameMode, Duration, IsRanked
- **MatchResult**: MatchId, PlayerId, Team, RatingBefore/After, IsWinner
- **CombatLogEntry**: MatchId, Timestamp, SourcePlayerId, TargetPlayerId, Ability, DamageDone, HealingDone, CrowdControl

### Database Constraints

- `Match.UniqueHash` unique index
- `MatchResult (MatchId, PlayerId)` unique index
- `CombatLogEntry` uses restricted deletes for Source/Target players

## Authentication

### Auth Microservice

The `AuthService` provides isolated authentication/authorization:

- ASP.NET Core Identity + EF Core (SQL Server) for user management and refresh-token storage
- Automatic migrations on startup (`AuthDbContext.Database.Migrate()`)
- JWT issuance with configurable issuer/audience/signing key

### Endpoints

Base URL: `http://localhost:8081/api/auth`

| Method | Path       | Description                                    |
|--------|------------|------------------------------------------------|
| POST   | `/register` | Create a user (email + password + optional full name) |
| POST   | `/login`    | Validate credentials and return tokens         |
| POST   | `/refresh`  | Exchange a refresh token for a new access token |

**Response Format:**
```json
{
  "accessToken": "{JWT}",
  "accessTokenExpiresAt": "2025-11-07T12:34:56Z",
  "refreshToken": "{opaque}",
  "refreshTokenExpiresAt": "2025-11-14T12:34:56Z"
}
```

Errors are returned as `400 Bad Request` with `{ "error": "message" }`.

All `register/login/refresh` routes are `[AllowAnonymous]`; any future authenticated endpoints can rely on `[Authorize]`.

### Authentication Workflow

1. **Register or login** against the auth service to obtain tokens:
   ```bash
   curl -X POST http://localhost:8081/api/auth/login \
        -H "Content-Type: application/json" \
        -d '{"email":"user@example.com","password":"[YOUR_PASSWORD]"}'
   ```

2. **Use the access token** for authenticated requests:
   ```bash
   curl http://localhost:8080/api/matches \
        -H "Authorization: Bearer <access_token>"
   ```

3. **Refresh expired tokens**:
   ```bash
   curl -X POST http://localhost:8081/api/auth/refresh \
        -H "Content-Type: application/json" \
        -d '{"refreshToken":"<refresh_token>"}'
   ```

**Note:** Refresh tokens are single-use; the previous token is revoked when exchanged.

## API Reference

Base path: `/api`

### Authentication

All endpoints (except `GET /api/health`) require authentication. Include the `Authorization: Bearer <access_token>` header obtained from the auth service.

### Endpoints

| Method | Path                    | Description                                    |
|--------|-------------------------|------------------------------------------------|
| GET    | `/api/health`           | Basic status (unauthenticated)                 |
| GET    | `/api/players`           | List all players                               |
| GET    | `/api/players/{id}`      | Get player by ID                               |
| POST   | `/api/players`           | Create a player                                |
| PUT    | `/api/players/{id}`      | Update a player                                |
| DELETE | `/api/players/{id}`      | Delete a player                                |
| GET    | `/api/matches`           | List all matches                               |
| GET    | `/api/matches/{id}`      | Get match by ID                                |
| POST   | `/api/matches`           | Create a match                                 |
| PUT    | `/api/matches/{id}`      | Update a match                                 |
| DELETE | `/api/matches/{id}`      | Delete a match                                 |
| GET    | `/api/matchresults`      | List all match results                         |
| GET    | `/api/matchresults/{id}` | Get match result by ID                         |
| POST   | `/api/matchresults`      | Create a match result                          |
| PUT    | `/api/matchresults/{id}` | Update a match result                          |
| DELETE | `/api/matchresults/{id}` | Delete a match result                          |
| GET    | `/api/combatlogentries`  | List all combat log entries                    |
| GET    | `/api/combatlogentries/{id}` | Get combat log entry by ID                 |
| POST   | `/api/combatlogentries`  | Create a combat log entry                      |
| PUT    | `/api/combatlogentries/{id}` | Update a combat log entry                  |
| DELETE | `/api/combatlogentries/{id}` | Delete a combat log entry                  |
| POST   | `/api/logs/upload`       | Upload a WoW combat log file (`multipart/form-data`, field `file`) |

**Upload Endpoint Behavior:**
- Responds `201 Created` with `Location: /api/matches/{id}` when a match was persisted
- Responds `202 Accepted` when no match could be inferred from the file

### OpenAPI Documentation

OpenAPI (Swagger) is enabled in Development: browse to `/openapi/v1.json` (UI mapping is present in dev).

## Setup & Configuration

### Local Development (dotnet run)

**Prerequisites:**
- .NET 9 SDK
- PostgreSQL 16+ (for analytics)
- SQL Server 2022+ (for authentication)

#### AuthService.Api

1. **Start SQL Server** (or use Docker):
   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=[YOUR_SA_PASSWORD]" \
        -p 1433:1433 --name auth-sql \
        mcr.microsoft.com/mssql/server:2022-latest
   ```

2. **Configure User Secrets** for sensitive configuration:
   ```bash
   cd AuthService.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
        "Server=localhost,1433;Database=AuthServiceDev;User Id=sa;Password=[YOUR_SA_PASSWORD];TrustServerCertificate=True"
   dotnet user-secrets set "Jwt:SigningKey" "[YOUR_JWT_SIGNING_KEY]"
   ```
   > Note: The `appsettings.Development.json` contains placeholder values; User Secrets override these values to ensure no sensitive data is committed to source control.

3. **Start the auth service:**
   ```bash
   dotnet run --project AuthService.Api --urls http://localhost:8081
   ```

#### PvpAnalytics.Api

1. **Start PostgreSQL** (or use Docker):
   ```bash
   docker run -e POSTGRES_USER=pvp -e POSTGRES_PASSWORD=[YOUR_POSTGRES_PASSWORD] \
        -e POSTGRES_DB=pvpdb -p 5442:5432 --name pvpdb \
        postgres:16
   ```

2. **Configure connection string** in `PvpAnalytics.Api/appsettings.Development.json`:
   - Set PostgreSQL connection string
   - Configure JWT settings to match Auth Service (issuer/audience/key)
   - Optionally use User Secrets for JWT configuration:
     ```bash
     cd PvpAnalytics.Api
     dotnet user-secrets set "Jwt:SigningKey" "[YOUR_JWT_SIGNING_KEY]"
     ```

3. **Start the analytics API:**
   ```bash
   dotnet run --project PvpAnalytics.Api --urls http://localhost:8080
   ```

### Docker Compose

**Services:**
- `db` (Postgres 16) – Analytics store, exposed on host port 5433
- `auth-sql` (SQL Server 2022) – Auth store, exposed on host port 1433
- `pvpanalytics` (analytics API) – Exposed on host port 8080
- `auth` (auth microservice) – Exposed on host port 8081

**Environment Configuration:**

Create a `.env` file in the project root:
```bash
SA_PASSWORD=[YOUR_SA_PASSWORD]
JWT_SIGNING_KEY=[YOUR_JWT_SIGNING_KEY]
```

**Analytics API Environment Variables:**
- `ConnectionStrings__DefaultConnection=Host=db;Port=5432;Username=pvp;Password=[YOUR_POSTGRES_PASSWORD];Database=pvpdb`
- `ASPNETCORE_URLS=http://+:8080`

**Auth API Environment Variables:**
- `ConnectionStrings__DefaultConnection=Server=auth-sql,1433;Database=AuthService;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True`
- `ASPNETCORE_URLS=http://+:8081`
- `Jwt__Issuer=PvpAnalytics.Auth`
- `Jwt__Audience=PvpAnalytics.Api`
- `Jwt__SigningKey=${JWT_SIGNING_KEY}`
- `Jwt__AccessTokenMinutes=60`
- `Jwt__RefreshTokenDays=7`

**Important Notes:**
- The Postgres container mounts `docker/postgres/init.sql` to create the analytics schema on first run
- SQL Server data files persist in the named volume `auth-sql-data`
- Migrations run automatically at container start (`Database.Migrate()`)

**JWT Configuration:**
JWT options must match between services:
- `Issuer`, `Audience`, `SigningKey`, `AccessTokenMinutes`, `RefreshTokenDays`
- In Docker: `JWT_SIGNING_KEY` environment variable (from `.env` file)

## Log Upload & Parsing

### Upload Endpoint

`POST /api/logs/upload` with `multipart/form-data` (field `file`)

### Parser Features

Located in `PvpAnalytics.Application/Logs`:

- **Language-agnostic**: Relies on numeric IDs and structured fields
- **Field mappings**: Uses `CombatLogFieldMappings` and `CombatLogEventTypes`
- **Timestamp handling**: Treated as local time and normalized to UTC (`AssumeLocal | AdjustToUniversal`)
- **Absorbed amounts**: Parsed from `SpellAbsorbed.Amount` (null if missing/unparseable)
- **Player resolution**: Normalize `Name-Realm` to `Name`, cache hits avoid repeated DB calls
- **Entry validation**: Entries are only persisted when a valid `SourcePlayerId` exists (no FK=0)

### Match Detection

The `CombatLogIngestionService` handles match detection:

- **Arena session detection**: Uses `ZONE_CHANGE` with known zone IDs (`ArenaZoneIds`)
- **Buffering**: Buffers entries and participants during an arena session
- **Finalization**: Finalizes a `Match` and `MatchResult`s on zone switch or EOF
- **Game mode derivation**: Uses `GameModeHelper` based on participant count:
  - 4 → TwoVsTwo
  - 6 → ThreeVsThree (or Shuffle if arenaMatchId contains "shuffle")
  - 10 → Skirmish (fallback; enum lacks 5v5)
  - otherwise → TwoVsTwo (default)

### Spell Mappings

Player class, spec, and faction detection uses static spell mappings in `PvpAnalytics.Core/Logs/PlayerAttributeMappings.cs`:

- **Target Expansion**: World of Warcraft: The War Within (Patch 11.0.x) as of November 2024
- **Maintenance**: See `SPELL_MAINTENANCE.md` for the process to update mappings when Blizzard releases new patches
- **Priority System**: Spec detection uses priority-based matching to handle multiple spell matches deterministically

## UI Frontend

The `PvpAnalytics.UI/` directory contains a standalone Vite + React (TypeScript) dashboard that visualizes arena statistics.

### Styling

- **Tailwind CSS**: Utility-first styling with global tokens (colors, typography, spacing) in `tailwind.config.js`
- **Responsive design**: Breakpoints applied directly in component class names for desktop and mobile layouts
- **Custom effects**: Gradients and glassmorphism accents implemented with Tailwind utilities plus inline styles where necessary

### Installation & Running

```bash
cd PvpAnalytics.UI
npm install
npm run dev
```

Navigate to the printed Vite URL (default `http://localhost:5173`). The page renders mock data while the analytics API endpoints are stabilized.

### Switching to Live Data

1. **Configure the analytics API base URL** via environment variable:
   ```bash
   # macOS / Linux
   export VITE_ANALYTICS_API_BASE_URL=http://localhost:8080/api
   
   # Windows PowerShell
   $Env:VITE_ANALYTICS_API_BASE_URL = 'http://localhost:8080/api'
   ```

2. **API Integration**: Uncomment the `axios.get` block inside `PvpAnalytics.UI/src/store/statsStore.ts` if you need to bypass the mock fallback entirely. By default, the store attempts to call the API when `VITE_ANALYTICS_API_BASE_URL` is present, otherwise it serves the mocked dataset defined in `PvpAnalytics.UI/src/mocks/playerStats.ts`.

3. **Search functionality**: `Search` triggers `loadStats(<playerIdOrQuery>)`; adjust the wrapper endpoint once a search API is available.

### Production Build

```bash
npm run build
npm run lint
```

## Testing

Run all backend unit and integration tests:

```bash
dotnet test
```

**Test Coverage:**
- `PvpAnalytics.Tests`: Combat-log parsing/ingestion (unit tests) and API integration tests that exercise `POST /api/logs/upload` using a stub authentication handler
- Auth microservice integration tests: Use EF Core's in-memory provider with a lightweight test factory. They validate the `register`, `login`, and `refresh` flows and assert error handling for duplicate or invalid credentials
- Structured logging: Test harnesses inject structured logging, so failures surface actionable context in the console when assertions fail

## Development Notes

### Architecture Patterns

- **Generic repository** (`IRepository<TEntity>`) with optional `autoSave` parameter on `Add/Update/Delete` for unit-of-work batching
- **CRUD services** implement `ICrudService<TEntity>` and are registered in `AddApplication()`
- **API controllers** are minimal and operate directly on entities (DTOs can be added later)
- **Health endpoint**: `GET /api/health`

### Authentication

- AuthService uses ASP.NET Core Identity with JWT + refresh tokens
- Extend via `IdentityService` if additional policies/claims are needed

### Logging

- Structured logging is enabled in ingestion and auth paths (`LogsController`, `CombatLogIngestionService`, `AuthController`, `IdentityService`) to surface key events and failure diagnostics

### API Documentation

- Postman collection example is provided in earlier conversation; generate via OpenAPI if needed

## License

MIT (or your preferred license)
