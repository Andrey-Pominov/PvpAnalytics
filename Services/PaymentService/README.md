# PaymentService

Payment processing microservice for PvpAnalytics platform. Handles payment transactions, payment status tracking, and payment history management.

## Overview

PaymentService provides isolated payment processing functionality using ASP.NET Core with PostgreSQL. It manages payment transactions, tracks payment status, and provides RESTful APIs for payment operations.

## Architecture

The service follows Clean Architecture principles with clear separation of concerns:

```text
PaymentService.Api/              # Presentation layer (Controllers, Program.cs)
PaymentService.Application/      # Business logic (Services, DTOs, Models, Abstractions)
PaymentService.Infrastructure/   # Data access (EF Core, DbContext, Repository implementations)
PaymentService.Core/            # Domain entities (Payment, PaymentStatus enum)
```

### Data Transfer Objects (DTOs)

The service uses DTOs to enforce field restrictions and prevent clients from modifying immutable or server-managed fields:

- **`CreatePaymentRequest`** (`PaymentService.Application/DTOs/CreatePaymentRequest.cs`)
  - Exposes only writable fields: `amount`, `transactionId`, `paymentMethod`, `description`
  - Prevents inclusion of immutable fields (`id`, `userId`, `status`, `createdAt`, `updatedAt`)
  
- **`UpdatePaymentRequest`** (`PaymentService.Application/DTOs/UpdatePaymentRequest.cs`)
  - Exposes only updatable fields: `amount`, `status`, `description`
  - Prevents inclusion of immutable fields (`id`, `userId`, `transactionId`, `paymentMethod`, `createdAt`, `updatedAt`)

- **`PaginatedResponse<T>`** (`PaymentService.Application/Models/PaginatedResponse.cs`)
  - Wraps paginated results with metadata: `items`, `total`, `page`, `pageSize`, `totalPages`
  - Used by `GET /api/payment` endpoint

### Field Enforcement Strategy

1. **DTO Structure**: DTOs only expose allowed fields, preventing clients from including immutable fields
2. **Server-Side Validation**: ASP.NET Core model validation ensures DTOs conform to their schema
3. **Selective Updates**: Update operations load existing entities and modify only allowed fields
4. **Server-Managed Fields**: Fields like `userId`, `status`, `createdAt`, `updatedAt` are set by the server, not clients

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

**Request/Response DTOs:**
- `CreatePaymentRequest` - Used for creating payments (only exposes writable fields)
- `UpdatePaymentRequest` - Used for updating payments (only exposes updatable fields)
- `PaginatedResponse<Payment>` - Paginated response wrapper with metadata

**Field Restrictions:**
- Immutable fields (`id`, `userId`, `transactionId`, `paymentMethod`, `createdAt`) cannot be included in request DTOs
- Server-managed fields (`userId`, `status`, `createdAt`, `updatedAt`) are automatically set by the server
- DTOs enforce field restrictions at the API layer, preventing clients from modifying immutable fields

### GET /api/payment

Get payments with pagination, filtering, and sorting support.

**Query Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | integer | 1 | Page number (1-based) |
| `pageSize` | integer | 20 | Number of items per page (max: 100) |
| `userId` | string | - | Filter by user ID (automatically applied for non-admin users) |
| `status` | integer | - | Filter by payment status (0=Pending, 1=Processing, 2=Completed, 3=Failed, 4=Cancelled, 5=Refunded) |
| `startDate` | datetime | - | Filter payments created on or after this date (ISO 8601 format) |
| `endDate` | datetime | - | Filter payments created on or before this date (ISO 8601 format) |
| `sortBy` | string | "createdAt" | Field to sort by (id, amount, status, userId, createdAt, updatedAt) |
| `sortOrder` | string | "desc" | Sort order: "asc" or "desc" |

**Examples:**

```
GET /api/payment?page=1&pageSize=20
GET /api/payment?status=2&sortBy=createdAt&sortOrder=desc
GET /api/payment?startDate=2025-01-01T00:00:00Z&endDate=2025-12-31T23:59:59Z
GET /api/payment?userId=user123&status=2&page=1&pageSize=10
```

**Response:** `200 OK`
```json
{
  "items": [
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
  ],
  "total": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

**Notes:**
- Regular users automatically see only their own payments (userId filter is applied automatically)
- Admin users can see all payments and can filter by any userId
- Maximum `pageSize` is 100 records per request
- Default `pageSize` is 20 if not specified
- Date filters use UTC timezone
- Response uses `PaginatedResponse<Payment>` wrapper with pagination metadata (`items`, `total`, `page`, `pageSize`, `totalPages`)

### GET /api/payment/{id}

Get a payment by ID.

**Response:** `200 OK` (same format as above)

**Error:** `404 Not Found` if payment doesn't exist

### POST /api/payment

Create a new payment.

**Request DTO:** `CreatePaymentRequest`

**Request Schema:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `amount` | decimal | Yes | Payment amount (0.01 to 999,999,999.99) |
| `transactionId` | string | Yes | Unique transaction identifier (must be unique across all payments) |
| `paymentMethod` | string | Yes | Payment method (e.g., "CreditCard", "PayPal", "BankTransfer") |
| `description` | string | No | Optional payment description (max 500 characters) |

**Server-Managed Fields (Automatically Set - Cannot Be Included in Request):**
- `id` - Auto-generated by the database (rejected if included)
- `userId` - Derived from the authenticated JWT token (automatically set from the current user, rejected if included)
- `status` - Defaults to `Pending` (0) on creation, not client-settable (rejected if included)
- `createdAt` - Automatically set to current UTC timestamp (rejected if included)
- `updatedAt` - Set to null on creation (rejected if included)

**Field Enforcement:**
The `CreatePaymentRequest` DTO only exposes writable fields. Any attempt to include immutable or server-managed fields will result in a `400 Bad Request` validation error, as these fields are not part of the DTO schema.

**Request Example:**
```json
{
  "amount": 29.99,
  "transactionId": "txn_abc123",
  "paymentMethod": "CreditCard",
  "description": "Premium subscription"
}
```

**Response:** `201 Created` with `Location: /api/payment/{id}` header

**Response Body:**
```json
{
  "id": 1,
  "amount": 29.99,
  "status": 0,
  "userId": "user123",
  "transactionId": "txn_abc123",
  "paymentMethod": "CreditCard",
  "createdAt": "2025-11-18T12:00:00Z",
  "updatedAt": null,
  "description": "Premium subscription"
}
```

**Errors:**
- `400 Bad Request` - Validation fails (missing required fields, invalid amount, duplicate transactionId, invalid field names, etc.)
- `401 Unauthorized` - User ID claim not found in token

**Notes:**
- **Required Fields**: `amount`, `transactionId`, and `paymentMethod` must be provided
- **Optional Fields**: `description` is optional
- **DTO Enforcement**: The `CreatePaymentRequest` DTO structure prevents clients from including `id`, `userId`, `status`, `createdAt`, or `updatedAt` - these fields are not part of the DTO schema
- **User ID**: Automatically derived from the authenticated JWT token (`sub` or `NameIdentifier` claim) and set by the server
- **Status**: Always defaults to `Pending` (0) on creation, set by the server regardless of any value provided
- **Transaction ID**: Must be unique across all payments. Duplicate transaction IDs will result in a database constraint violation error
- **Created At**: Automatically set to the current UTC timestamp by the server (not the database default, to ensure consistency)

### PUT /api/payment/{id}

Update an existing payment.

**Request DTO:** `UpdatePaymentRequest`

**Writable Fields (Only These Fields Can Be Updated):**
- `amount` - Payment amount (0.01 to 999,999,999.99)
- `status` - Payment status (0=Pending, 1=Processing, 2=Completed, 3=Failed, 4=Cancelled, 5=Refunded)
- `description` - Payment description (max 500 characters, optional)

**Immutable Fields (Cannot Be Included in Request DTO):**
- `id` - Cannot be changed (use URL parameter, rejected if included in body)
- `userId` - Cannot be changed (ownership is fixed, rejected if included)
- `transactionId` - Cannot be changed (immutable identifier, rejected if included)
- `paymentMethod` - Cannot be changed (set at creation, rejected if included)
- `createdAt` - Cannot be changed (creation timestamp, rejected if included)
- `updatedAt` - Automatically managed by server (set to current UTC time on update, rejected if included)

**Request Body:**
The request body uses the `UpdatePaymentRequest` DTO which only exposes writable fields. The DTO structure prevents clients from including immutable fields (`id`, `userId`, `transactionId`, `paymentMethod`, `createdAt`, `updatedAt`). The `id` is specified in the URL path parameter, not in the request body.

**Field Enforcement:**
The `UpdatePaymentRequest` DTO only exposes updatable fields. Any attempt to include immutable fields will result in a `400 Bad Request` validation error, as these fields are not part of the DTO schema. The server performs selective updates, modifying only the fields present in the DTO and leaving immutable fields unchanged.

**Request Example:**
```json
{
  "amount": 39.99,
  "status": 2,
  "description": "Updated premium subscription"
}
```

**Response:** `204 No Content`

**Errors:**
- `400 Bad Request` - Returned when:
  - Invalid field names are included in the request body (immutable fields are not part of the DTO schema)
  - Validation fails (invalid amount, invalid status, etc.)
- `403 Forbid` - User does not have permission to update this payment (not the owner and not an admin)
- `404 Not Found` - Payment with the specified ID does not exist
- `401 Unauthorized` - User ID claim not found in token

**Notes:**
- **DTO Enforcement**: The `UpdatePaymentRequest` DTO structure prevents clients from including `id`, `userId`, `transactionId`, `paymentMethod`, `createdAt`, or `updatedAt` - these fields are not part of the DTO schema
- **Selective Updates**: The server performs selective updates by:
  1. Loading the existing payment entity from the database
  2. Updating only the fields present in the `UpdatePaymentRequest` DTO (`amount`, `status`, `description`)
  3. Setting `updatedAt` to the current UTC timestamp
  4. Leaving all other fields (including immutable ones) unchanged
- **Ownership**: Regular users can only update their own payments. Admin users can update any payment
- **Immutable Fields**: Cannot be modified - the DTO structure prevents these fields from being included in requests
- **Updated At**: Automatically set to the current UTC timestamp by the server on successful update
- **Status Changes**: Status can be updated to any valid PaymentStatus value

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
```bash
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

> ⚠️ **Security Warning**: Never commit `.env` files or secret keys to version control. Ensure `.env` is in your `.gitignore` file.

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

> ⚠️ **Production Security Warning**: The above Docker Compose example uses plaintext environment variables for secrets (e.g., `JWT_SIGNING_KEY`), which is **acceptable for local development only**. **Never pass secrets as plaintext environment variables in production environments.** For production deployments, use proper secret management solutions such as:
> - **Docker Secrets** (Docker Swarm)
> - **Kubernetes Secrets** (Kubernetes)
> - **Cloud Secret Managers**: AWS Secrets Manager, Azure Key Vault, Google Secret Manager
> - **HashiCorp Vault** (self-hosted or cloud)
> - **Environment-specific secret injection** via CI/CD pipelines

Then start the service:
```bash
docker compose up -d payment
```

## Security Features

- **JWT Authentication**: All endpoints require valid JWT tokens
- **Input Validation**: Data annotations validate payment data via DTOs
- **Field Restrictions**: DTOs enforce field-level restrictions, preventing modification of immutable fields
- **User-Scoped Access**: Non-admin users can only access/modify their own payments
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
- **DTOs**: Controllers use `CreatePaymentRequest` and `UpdatePaymentRequest` DTOs to enforce field restrictions
- **Pagination**: `GET /api/payment` uses `PaginatedResponse<Payment>` wrapper with query parameters for pagination, filtering, and sorting
- **Field Enforcement**: Immutable fields are prevented at the DTO level - clients cannot include `id`, `userId`, `transactionId`, `paymentMethod`, `createdAt`, or `updatedAt` in requests
- **Selective Updates**: Update operations load existing entities and modify only allowed fields, preventing modification of immutable fields
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

