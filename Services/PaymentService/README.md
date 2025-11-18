# PaymentService

Payment processing microservice for PvpAnalytics platform. Handles payment transactions, payment status tracking, and payment history management.

## Overview

PaymentService provides isolated payment processing functionality using ASP.NET Core with PostgreSQL. It manages payment transactions, tracks payment status, and provides RESTful APIs for payment operations.

## Architecture

The service follows Clean Architecture principles with clear separation of concerns:

```
PaymentService.Api/              # Presentation layer (Controllers, Program.cs)
PaymentService.Application/      # Business logic (Services, DTOs, Abstractions)
PaymentService.Infrastructure/   # Data access (EF Core, DbContext, Repository implementations)
PaymentService.Core/            # Domain entities (Payment, PaymentStatus enum)
```

## Features

- **Payment Management**: Create, read, update, and delete payment records
- **Payment Status Tracking**: Track payment status (Pending, Processing, Completed, Failed, Cancelled, Refunded)
- **Transaction Management**: Unique transaction ID tracking for payment reconciliation
- **User Payment History**: Track payments by user ID
- **JWT Authentication**: Secure token-based authentication
- **RESTful API**: Full CRUD operations for payments

## API Endpoints

Base URL: `http://localhost:8082/api/payment`

All endpoints require JWT authentication. Include the `Authorization: Bearer <access_token>` header obtained from the auth service.

### GET /api/payment

Get all payments.

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "amount": 29.99,
    "status": 2,
    "userId": "user123",
    "transactionId": "txn_abc123",
    "paymentMethod": "CreditCard",
    "createdAt": "2025-11-18T12:00:00Z",
    "updatedAt": null,
    "description": "Premium subscription"
  }
]
```

### GET /api/payment/{id}

Get a payment by ID.

**Response:** `200 OK` (same format as above)

**Error:** `404 Not Found` if payment doesn't exist

### POST /api/payment

Create a new payment.

**Request:**
```json
{
  "amount": 29.99,
  "status": 0,
  "userId": "user123",
  "transactionId": "txn_abc123",
  "paymentMethod": "CreditCard",
  "description": "Premium subscription"
}
```

**Response:** `201 Created` with `Location: /api/payment/{id}` header

**Error:** `400 Bad Request` if validation fails

**Note:** The `id` field should not be set when creating a new payment. `createdAt` is automatically set to the current UTC time.

### PUT /api/payment/{id}

Update an existing payment.

**Request:**
```json
{
  "id": 1,
  "amount": 29.99,
  "status": 2,
  "userId": "user123",
  "transactionId": "txn_abc123",
  "paymentMethod": "CreditCard",
  "description": "Premium subscription"
}
```

**Response:** `204 No Content`

**Error:** 
- `400 Bad Request` if ID mismatch
- `404 Not Found` if payment doesn't exist

**Note:** `updatedAt` is automatically set to the current UTC time.

### DELETE /api/payment/{id}

Delete a payment.

**Response:** `204 No Content`

**Error:** `404 Not Found` if payment doesn't exist

## Payment Status Enum

| Value | Status      | Description                          |
|-------|-------------|--------------------------------------|
| 0     | Pending     | Payment initiated but not processed  |
| 1     | Processing  | Payment is being processed           |
| 2     | Completed   | Payment successfully completed        |
| 3     | Failed      | Payment processing failed            |
| 4     | Cancelled   | Payment was cancelled                |
| 5     | Refunded    | Payment was refunded                 |

## Configuration

### Environment Variables

Required environment variables (set in `.env` file or Docker Compose):

- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
- `Jwt__Issuer`: JWT issuer (must match AuthService, e.g., "PvpAnalytics.Auth")
- `Jwt__Audience`: JWT audience (must match AuthService, e.g., "PvpAnalytics.Api")
- `Jwt__SigningKey`: JWT signing key (must match AuthService)
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc.: CORS allowed origins

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

Or via environment variables:
```
Cors__AllowedOrigins__0=http://localhost:3000
Cors__AllowedOrigins__1=https://localhost:8080
```

## Database

- **Database**: PostgreSQL 16+
- **Schema**: Managed via EF Core Migrations
- **Auto-migration**: Migrations run automatically on startup
- **Tables**:
  - `Payments`: Payment records with the following indexes:
    - Unique index on `TransactionId`
    - Index on `UserId` for user payment queries
    - Index on `CreatedAt` for time-based queries

### Payment Entity Schema

- `Id` (long): Primary key
- `Amount` (decimal): Payment amount with precision 18,2
- `Status` (PaymentStatus enum): Current payment status
- `UserId` (string): User identifier who made the payment
- `TransactionId` (string): Unique transaction identifier
- `PaymentMethod` (string): Payment method used (e.g., "CreditCard", "PayPal")
- `CreatedAt` (DateTime): Payment creation timestamp (UTC)
- `UpdatedAt` (DateTime?): Last update timestamp (UTC), nullable
- `Description` (string?): Optional payment description

## Running Locally

### Prerequisites

- .NET 9 SDK
- PostgreSQL 16+ (or Docker)

### Steps

1. **Start PostgreSQL** (if not using Docker):
   ```bash
   docker run -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
        -e POSTGRES_DB=paymentdb -p 5432:5432 --name paymentdb \
        postgres:16
   ```

2. **Configure connection string** in `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=paymentdb"
     }
   }
   ```

3. **Configure JWT settings** to match AuthService:
   ```json
   {
     "Jwt": {
       "Issuer": "PvpAnalytics.Auth",
       "Audience": "PvpAnalytics.Api",
       "SigningKey": "[YOUR_JWT_SIGNING_KEY]"
     }
   }
   ```
   
   Or use User Secrets:
   ```bash
   cd Services/PaymentService/PaymentService.Api
   dotnet user-secrets set "Jwt:SigningKey" "[YOUR_JWT_SIGNING_KEY]"
   ```

4. **Run the service**:
   ```bash
   dotnet run --project Services/PaymentService/PaymentService.Api --urls http://localhost:8082
   ```

## Running with Docker

The service can be added to the main `docker-compose.yaml`. Ensure your `.env` file contains:

```bash
JWT_SIGNING_KEY=YourSecretKeyHere
```

Example Docker Compose configuration:

```yaml
payment:
  image: paymentservice
  build:
    context: .
    dockerfile: Services/PaymentService/PaymentService.Api/Dockerfile
  ports:
    - "8082:8082"
  environment:
    - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Username=pvp;Password=pvp123;Database=paymentdb
    - ASPNETCORE_ENVIRONMENT=Production
    - ASPNETCORE_URLS=http://+:8082
    - Jwt__Issuer=PvpAnalytics.Auth
    - Jwt__Audience=PvpAnalytics.Api
    - Jwt__SigningKey=${JWT_SIGNING_KEY?JWT_SIGNING_KEY not set}
    - Cors__AllowedOrigins__0=http://localhost:3000
    - Cors__AllowedOrigins__1=https://localhost:8080
  depends_on:
    - db
```

Then start the service:
```bash
docker compose up -d payment
```

## Security Features

- **JWT Authentication**: All endpoints require valid JWT tokens
- **Input Validation**: Data annotations validate payment data
- **CORS Protection**: Configurable allowed origins
- **Unique Transaction IDs**: Prevents duplicate payment processing
- **Audit Trail**: CreatedAt and UpdatedAt timestamps track payment lifecycle

## Dependencies

- **ASP.NET Core**: Web framework
- **Entity Framework Core**: Data access and migrations
- **PostgreSQL**: Database provider (Npgsql)
- **JWT Bearer Authentication**: Token-based authentication
- **PvpAnalytics.Shared**: Shared security configuration (JWT options, CORS options)

## Testing

Integration tests can be added to `Tests/PvpAnalytics.Tests/Payment/`:

```bash
dotnet test Tests/PvpAnalytics.Tests/PvpAnalytics.Tests.csproj --filter "FullyQualifiedName~Payment"
```

## Development Notes

- All endpoints require `[Authorize]` attribute
- Payment IDs are auto-generated by the database
- Transaction IDs must be unique (enforced by database index)
- Payment status transitions should be validated in business logic (future enhancement)
- Consider adding payment method validation and payment gateway integration (future enhancement)
- The service automatically creates the database and runs migrations on startup

## Future Enhancements

- Payment gateway integration (Stripe, PayPal, etc.)
- Payment webhook handling
- Payment status transition validation
- Payment refund processing
- Payment reporting and analytics
- Payment method validation
- Recurring payment support

