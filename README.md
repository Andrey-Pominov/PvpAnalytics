# PvpAnalytics

**Author:** Rmpriest

PvP combat analytics platform for World of Warcraft combat logs. A microservices-based solution that parses arena matches, stores player data, and provides analytics through REST APIs.

## Table of Contents

- [Project Overview](#project-overview)
- [Quick Start](#quick-start)
- [Project Structure](#project-structure)
- [Services](#services)
- [Architecture](#architecture)
- [Setup & Configuration](#setup--configuration)
- [Development](#development)
- [Testing](#testing)
- [License](#license)

## Project Overview

PvpAnalytics is a microservices platform that processes World of Warcraft combat log files to extract and analyze PvP arena match data. The platform consists of multiple services working together:

- **AuthService**: Handles user authentication and authorization
- **PvpAnalytics Service**: Processes combat logs and provides analytics APIs
- **UI**: React-based frontend dashboard

### Key Features

- **Combat Log Parsing**: Automatically parse WoW combat log files and extract arena matches
- **Player Management**: Store and query player information with automatic enrichment via WoW API
- **Match Analytics**: Track arena matches with ratings, results, and detailed combat data
- **RESTful APIs**: Full CRUD operations for all entities
- **JWT Authentication**: Secure token-based authentication with refresh tokens
- **Modern UI**: React + TypeScript dashboard for visualizing statistics

## Quick Start

### Docker Compose (Recommended)

1. Create a `.env` file in the project root:
   ```bash
   SA_PASSWORD=[YOUR_SA_PASSWORD]
   JWT_SIGNING_KEY=[YOUR_JWT_SIGNING_KEY]
   WowApi__ClientId=[YOUR_WOW_API_CLIENT_ID]
   WowApi__ClientSecret=[YOUR_WOW_API_CLIENT_SECRET]
   ```

2. Start all services:
   ```bash
   docker compose build
   docker compose up -d
   ```

3. Access the services:
   - Auth API: `http://localhost:8081`
   - Analytics API: `http://localhost:8080`
   - UI: `http://localhost:3000`

### Local Development

**Prerequisites:** .NET 9 SDK, PostgreSQL 16+, SQL Server 2022+

See [Setup & Configuration](#setup--configuration) for detailed setup instructions.

## Project Structure

The solution is organized into service folders for better clarity and scalability:

```
PvpAnalytics/
├── Services/
│   ├── AuthService/              # Authentication microservice
│   │   ├── AuthService.Api/
│   │   ├── AuthService.Application/
│   │   ├── AuthService.Core/
│   │   └── AuthService.Infrastructure/
│   │   └── README.md            # Service-specific documentation
│   │
│   └── PvpAnalytics/            # Analytics microservice
│       ├── PvpAnalytics.Api/
│       ├── PvpAnalytics.Application/
│       ├── PvpAnalytics.Core/
│       └── PvpAnalytics.Infrastructure/
│       └── README.md            # Service-specific documentation
│
├── Shared/
│   └── PvpAnalytics.Shared/     # Shared code (JWT options, CORS options)
│
├── Workers/
│   └── PvpAnalytics.Worker/    # Background workers
│
├── Tests/
│   └── PvpAnalytics.Tests/     # Integration and unit tests
│
└── ui/
    └── PvpAnalytics.UI/         # React frontend
```

## Services

### AuthService

Authentication and authorization microservice. Handles user registration, login, and token management.

**Documentation:** See [Services/AuthService/README.md](Services/AuthService/README.md)

**Key Features:**
- User registration and login
- JWT access tokens and refresh tokens
- ASP.NET Core Identity integration
- SQL Server database

**Endpoints:**
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Authenticate user
- `POST /api/auth/refresh` - Refresh access token

### PvpAnalytics Service

Core analytics service for processing combat logs and providing data APIs.

**Documentation:** See [Services/PvpAnalytics/README.md](Services/PvpAnalytics/README.md)

**Key Features:**
- Combat log parsing and ingestion
- Player data management
- Match tracking and analytics
- WoW API integration for player enrichment
- PostgreSQL database

**Endpoints:**
- Full CRUD APIs for Players, Matches, MatchResults, CombatLogEntries
- `POST /api/logs/upload` - Upload and process combat log files

## Architecture

### Clean Architecture

Each service follows Clean Architecture principles:

```
┌─────────────────────────────────┐
│   API Layer (Controllers)      │  ← HTTP endpoints
├─────────────────────────────────┤
│   Application Layer (Services)  │  ← Business logic
├─────────────────────────────────┤
│   Infrastructure Layer (EF)     │  ← Data access
├─────────────────────────────────┤
│   Core Layer (Entities)         │  ← Domain models
└─────────────────────────────────┘
```

### Key Patterns

- **Generic Repository**: `IRepository<TEntity>` with optional unit-of-work
- **CRUD Services**: `ICrudService<T>` for business logic
- **Dependency Injection**: Service registration via extension methods
- **Configuration**: Environment variables for sensitive data
- **CORS**: Configurable allowed origins
- **EF Core with PostgreSQL** for analytics data storage
- **SQL Server** for authentication data storage
- **Language-agnostic combat log parser** (uses numeric IDs and field mappings)

## Data Model

For detailed data model information, see [Services/PvpAnalytics/README.md](Services/PvpAnalytics/README.md#data-model).

**Core Entities:**
- **Player**: Character name, realm, class, spec, faction
- **Match**: Arena match metadata (map, game mode, duration, ratings)
- **MatchResult**: Player results per match (ratings, win/loss)
- **CombatLogEntry**: Granular combat log data (damage, healing, CC)

### Authentication

Authentication is handled by the **AuthService** microservice. See [Services/AuthService/README.md](Services/AuthService/README.md) for detailed documentation.

**Quick Start:**
1. Register a user: `POST http://localhost:8081/api/auth/register`
2. Login: `POST http://localhost:8081/api/auth/login`
3. Use access token: `Authorization: Bearer <token>`
4. Refresh token: `POST http://localhost:8081/api/auth/refresh`

### API Reference

For detailed API documentation, see:
- **AuthService APIs**: [Services/AuthService/README.md](Services/AuthService/README.md#api-endpoints)
- **PvpAnalytics APIs**: [Services/PvpAnalytics/README.md](Services/PvpAnalytics/README.md#api-endpoints)

**Quick Reference:**

**AuthService** (`http://localhost:8081/api/auth`):
- `POST /register` - Register new user
- `POST /login` - Authenticate user
- `POST /refresh` - Refresh access token

**PvpAnalytics** (`http://localhost:8080/api`):
- `GET /api/health` - Health check
- `GET /api/players` - List players
- `GET /api/matches` - List matches
- `POST /api/logs/upload` - Upload combat log file
- Full CRUD for Players, Matches, MatchResults, CombatLogEntries

**OpenAPI Documentation:**
- Available at `/openapi/v1.json` in Development mode

## Setup & Configuration

### Environment Variables

Create a `.env` file in the project root with the following variables:

```bash
# SQL Server (AuthService)
SA_PASSWORD=YourStrong@Password123

# JWT Configuration (shared between services)
JWT_SIGNING_KEY=YourSecretKeyHere

# WoW API (PvpAnalytics Service)
WowApi__ClientId=your-client-id
WowApi__ClientSecret=your-client-secret
```

**Important:** The `.env` file is gitignored and should never be committed. Keep your secrets secure!

### Docker Compose Setup

1. **Create `.env` file** with the variables above

2. **Start all services:**
   ```bash
   docker compose build
   docker compose up -d
   ```

3. **Verify services are running:**
   ```bash
   docker compose ps
   ```

**Services:**
- `auth` - AuthService API (port 8081)
- `pvpanalytics` - PvpAnalytics API (port 8080)
- `ui` - Nginx serving React UI (port 3000)
- `db` - PostgreSQL for analytics (port 5442)
- `auth-sql` - SQL Server for authentication (port 1433)

### Local Development

For local development setup, see:
- **AuthService**: [Services/AuthService/README.md](Services/AuthService/README.md#running-locally)
- **PvpAnalytics**: [Services/PvpAnalytics/README.md](Services/PvpAnalytics/README.md#running-locally)

## Development

### Project Structure

The solution uses a service-oriented folder structure:

- **Services/** - Individual microservices (AuthService, PvpAnalytics)
- **Shared/** - Shared code and utilities
- **Workers/** - Background worker services
- **Tests/** - Test projects
- **ui/** - Frontend application

### Adding a New Service

To add a new service (e.g., PaymentTransaction):

1. Create folder structure:
   ```
   Services/PaymentTransaction/
   ├── PaymentTransaction.Api/
   ├── PaymentTransaction.Application/
   ├── PaymentTransaction.Core/
   └── PaymentTransaction.Infrastructure/
   ```

2. Add projects to solution:
   ```bash
   dotnet sln add Services/PaymentTransaction/PaymentTransaction.Api/PaymentTransaction.Api.csproj
   # ... add other projects
   ```

3. Create service-specific README.md in the service folder

4. Update `compose.yaml` if the service needs Docker support

### Log Upload & Parsing

For detailed information about combat log parsing, see [Services/PvpAnalytics/README.md](Services/PvpAnalytics/README.md#combat-log-parsing).

**Quick Overview:**
- Upload endpoint: `POST /api/logs/upload`
- Automatically detects arena matches
- Enriches player data via WoW API
- Stores matches, players, and combat log entries

## UI Frontend

The `ui/PvpAnalytics.UI/` directory contains a React + TypeScript dashboard.

**Location:** `ui/PvpAnalytics.UI/`

**Tech Stack:**
- Vite + React + TypeScript
- Tailwind CSS for styling
- Responsive design

**Running Locally:**
```bash
cd ui/PvpAnalytics.UI
npm install
npm run dev
```

**Production Build:**
```bash
npm run build
npm run lint
```

The UI is served via Nginx in Docker at `http://localhost:3000`.

## Testing

Run all tests:
```bash
dotnet test
```

**Test Coverage:**
- **PvpAnalytics.Tests**: Combat log parsing, API integration tests
- **Auth Tests**: User registration, login, token refresh flows
- **Integration Tests**: Full API workflows with in-memory databases

## Additional Documentation

- **AuthService**: [Services/AuthService/README.md](Services/AuthService/README.md)
- **PvpAnalytics Service**: [Services/PvpAnalytics/README.md](Services/PvpAnalytics/README.md)
- **Spell Maintenance**: [SPELL_MAINTENANCE.md](SPELL_MAINTENANCE.md)

## License

MIT (or your preferred license)
