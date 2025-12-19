# AuthService

Authentication and authorization microservice for PvpAnalytics platform.

## Overview

AuthService provides isolated authentication and authorization functionality using ASP.NET Core Identity with JWT tokens and refresh tokens. It manages user accounts, handles registration, login, and token refresh operations.

## Architecture

The service follows Clean Architecture principles with clear separation of concerns:

```
AuthService.Api/              # Presentation layer (Controllers, Program.cs)
AuthService.Application/      # Business logic (Services, DTOs, Abstractions)
AuthService.Infrastructure/   # Data access (EF Core, DbContext, Repository implementations)
AuthService.Core/            # Domain entities (RefreshToken, etc.)
```

## Features

- **User Registration**: Create new user accounts with email and password
- **User Login**: Authenticate users and issue JWT access tokens and refresh tokens
- **Token Refresh**: Exchange refresh tokens for new access tokens
- **JWT Authentication**: Secure token-based authentication
- **Refresh Token Management**: Single-use refresh tokens with automatic revocation
- **Password Security**: ASP.NET Core Identity password hashing and validation

## API Endpoints

Base URL: `http://localhost:8081/api/auth`

### POST /register

Register a new user account.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "fullName": "John Doe" // Optional
}
```

**Response:** `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "accessTokenExpiresAt": "2025-11-18T13:00:00Z",
  "refreshToken": "abc123def456...",
  "refreshTokenExpiresAt": "2025-11-25T13:00:00Z"
}
```

**Error:** `400 Bad Request`
```json
{
  "error": "User already exists"
}
```

### POST /login

Authenticate an existing user.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:** `200 OK` (same format as register)

**Error:** `400 Bad Request`
```json
{
  "error": "Invalid credentials"
}
```

### POST /refresh

Refresh an expired access token using a refresh token.

**Request:**
```json
{
  "refreshToken": "abc123def456..."
}
```

**Response:** `200 OK` (same format as register/login)

**Error:** `400 Bad Request`
```json
{
  "error": "Invalid or expired refresh token"
}
```

**Note:** Refresh tokens are single-use. After successful refresh, the old token is revoked.

## Configuration

### Environment Variables

Required environment variables (set in `.env` file or Docker Compose):

- `ConnectionStrings__DefaultConnection`: Oracle Database connection string (format: `Data Source=host:port/service_name;User Id=username;Password=password;`)
- `Jwt__Issuer`: JWT issuer (e.g., "PvpAnalytics.Auth")
- `Jwt__Audience`: JWT audience (e.g., "PvpAnalytics.Api")
- `Jwt__SigningKey`: Secret key for signing JWTs
- `Jwt__AccessTokenMinutes`: Access token lifetime in minutes (default: 60)
- `Jwt__RefreshTokenDays`: Refresh token lifetime in days (default: 7)

### CORS Configuration

CORS origins are configured via `Cors:AllowedOrigins` in `appsettings.json` or environment variables:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://localhost:8080"
    ]
  }
}
```

Or via environment variable:
```
Cors__AllowedOrigins__0=http://localhost:3000
Cors__AllowedOrigins__1=https://localhost:8080
```

## Database

- **Database**: Oracle Database 23c Free
- **Schema**: Managed via EF Core Migrations
- **Auto-migration**: Migrations run automatically on startup
- **Tables**:
  - `Users`: User accounts (renamed from AspNetUsers)
  - `Roles`: User roles (renamed from AspNetRoles)
  - `RefreshTokens`: Refresh token storage

## Running Locally

### Prerequisites

- .NET 9 SDK
- Oracle Database 26ai (or Oracle Database 23c Free) - can use Docker

### Steps

1. **Start Oracle Database** (if not using Docker):
   ```bash
   docker run -d --name auth-oracle \
        -e ORACLE_PASSWORD=YourPassword123! \
        -e ORACLE_DATABASE=AuthService \
        -p 1521:1521 \
        gvenzl/oracle-xe:23-slim
   ```

2. **Configure connection string** in `appsettings.Development.json` or use User Secrets:
   ```bash
   cd Services/AuthService/AuthService.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
        "Data Source=localhost:1521/XE;User Id=system;Password=YourPassword123!;"
   dotnet user-secrets set "Jwt:SigningKey" "YourSecretKeyHere"
   ```

3. **Run the service**:
   ```bash
   dotnet run --project Services/AuthService/AuthService.Api --urls http://localhost:8081
   ```

## Running with Docker

The service is included in the main `docker-compose.yaml`. Ensure your `.env` file contains:

```bash
ORACLE_PASSWORD=YourPassword123!
JWT_SIGNING_KEY=YourSecretKeyHere
```

Then start all services:
```bash
docker compose up -d auth
```

## Security Features

- **Password Hashing**: Uses ASP.NET Core Identity's secure password hashing (PBKDF2)
- **JWT Signing**: HMAC-SHA256 signing with configurable secret key
- **Token Expiration**: Configurable access token and refresh token lifetimes
- **Single-Use Refresh Tokens**: Refresh tokens are revoked after use
- **CORS Protection**: Configurable allowed origins
- **Input Validation**: Email and password validation on registration/login

## Dependencies

- **ASP.NET Core Identity**: User management and authentication
- **Entity Framework Core**: Data access and migrations
- **Oracle Database 26ai**: Database provider (via Oracle.EntityFrameworkCore)
- **JWT Bearer Authentication**: Token-based authentication

## Testing

Integration tests are located in `Tests/PvpAnalytics.Tests/Auth/`:

```bash
dotnet test Tests/PvpAnalytics.Tests/PvpAnalytics.Tests.csproj --filter "FullyQualifiedName~Auth"
```

## Development Notes

- All endpoints are currently `[AllowAnonymous]` for development
- Future authenticated endpoints can use `[Authorize]` attribute
- Refresh tokens are stored in the database and validated on each refresh request
- The service automatically creates the database and runs migrations on startup

