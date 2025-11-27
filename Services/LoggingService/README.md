# LoggingService

Centralized logging microservice for the PvpAnalytics platform. Provides gRPC-based logging infrastructure with automatic service discovery and health monitoring.

## Overview

LoggingService is a dedicated microservice that collects, stores, and manages application logs from all services in the PvpAnalytics platform. It uses gRPC for high-performance communication and implements automatic service discovery to track registered services and their health status.

## Features

- **Centralized Logging**: All services send logs to a single LoggingService instance
- **gRPC Communication**: High-performance binary protocol for log transmission
- **Service Discovery**: Automatic registration and health monitoring of services
- **Heartbeat Mechanism**: Tracks service health with periodic heartbeats
- **Query Capabilities**: Query logs by service, level, date range, or user
- **PostgreSQL Storage**: Persistent log storage with optimized indexes

## Architecture

### Components

- **LoggingService.Api**: ASP.NET Core API with gRPC server
- **LoggingService.Application**: Business logic and gRPC service implementation
- **LoggingService.Infrastructure**: Data access layer with EF Core and PostgreSQL
- **LoggingService.Core**: Domain entities and DTOs

### Data Model

**ApplicationLog**: Stores individual log entries
- Timestamp, Level, ServiceName, Message
- Exception details, UserId, RequestPath, RequestMethod
- StatusCode, Duration, Properties (JSON)

**RegisteredService**: Tracks registered services
- ServiceName, Endpoint, Version
- RegisteredAt, LastHeartbeat, Status (Online/Offline)

## gRPC API

### Service Methods

**LoggingService** (gRPC service on port 50051):

- `CreateLog(CreateLogRequest) returns (LogEntryResponse)` - Create a log entry
- `GetLogs(LogQueryRequest) returns (LogQueryResponse)` - Query logs with filters
- `GetLogById(LogByIdRequest) returns (LogEntryResponse)` - Get specific log by ID
- `GetLogsByService(LogsByServiceRequest) returns (LogQueryResponse)` - Get logs for a service
- `GetLogsByLevel(LogsByLevelRequest) returns (LogQueryResponse)` - Get logs by level
- `RegisterService(ServiceRegistrationRequest) returns (ServiceRegistrationResponse)` - Register a service
- `Heartbeat(HeartbeatRequest) returns (HeartbeatResponse)` - Update service heartbeat

### Proto Definition

The gRPC service definition is located at:
- `Services/LoggingService/LoggingService.Core/Protos/logging.proto`
- `Shared/PvpAnalytics.Shared/Protos/logging.proto` (for clients)

## Service Discovery

### How It Works

1. **Registration**: When a service starts, it calls `RegisterService` with its name, endpoint, and version
2. **Heartbeat**: Services send periodic heartbeats (default: every 30 seconds) to maintain "Online" status
3. **Health Tracking**: LoggingService tracks the last heartbeat time and marks services as "Offline" if heartbeats stop
4. **Database Storage**: All registered services are persisted in the `RegisteredServices` table

### Registered Services

Services automatically register themselves on startup:
- **AuthService**: Authentication and authorization
- **PvpAnalytics**: Core analytics service
- **PaymentService**: Payment processing

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection=Host=logging-postgres;Port=5432;Database=LoggingService;Username=postgres;Password=${POSTGRES_PASSWORD}

# JWT (for REST API authentication)
Jwt__Issuer=PvpAnalytics
Jwt__Audience=PvpAnalytics
Jwt__SigningKey=${JWT_SIGNING_KEY}

# gRPC Endpoint (Kestrel configuration)
Kestrel__Endpoints__gRPC__Url=http://+:50051
Kestrel__Endpoints__gRPC__Protocols=Http2

# Service Discovery
ServiceDiscovery__HeartbeatIntervalSeconds=30
ServiceDiscovery__HeartbeatTimeoutSeconds=90
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=logging-postgres;Port=5432;Database=LoggingService;Username=postgres;Password=${POSTGRES_PASSWORD}"
  },
  "Jwt": {
    "Issuer": "PvpAnalytics",
    "Audience": "PvpAnalytics",
    "SigningKey": "${JWT_SIGNING_KEY}"
  },
  "Kestrel": {
    "Endpoints": {
      "gRPC": {
        "Url": "http://+:50051",
        "Protocols": "Http2"
      }
    }
  },
  "ServiceDiscovery": {
    "HeartbeatIntervalSeconds": 30,
    "HeartbeatTimeoutSeconds": 90
  }
}
```

## Integration

### For Other Services

To integrate LoggingService into a new service:

1. **Add Shared Project Reference**:
   ```xml
   <ProjectReference Include="..\..\..\Shared\PvpAnalytics.Shared\PvpAnalytics.Shared.csproj" />
   ```

2. **Register LoggingClient in DI**:
   ```csharp
   builder.Services.AddSingleton<ILoggingClient>(sp =>
   {
       var config = sp.GetRequiredService<IConfiguration>();
       var logger = sp.GetRequiredService<ILogger<LoggingClient>>();
       return new LoggingClient(config, logger);
   });
   ```

3. **Register Service on Startup**:
   ```csharp
   var loggingClient = app.Services.GetRequiredService<ILoggingClient>();
   var serviceName = builder.Configuration["LoggingService:ServiceName"] ?? "YourService";
   var serviceEndpoint = builder.Configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault();
   var serviceVersion = "1.0.0";

   await loggingClient.RegisterServiceAsync(serviceName, serviceEndpoint, serviceVersion);
   
   var heartbeatInterval = TimeSpan.FromSeconds(
       builder.Configuration.GetValue<int>("LoggingService:HeartbeatIntervalSeconds", 30));
   loggingClient.StartHeartbeat(serviceName, heartbeatInterval);
   ```

4. **Add Configuration**:
   ```json
   {
     "LoggingService": {
       "GrpcEndpoint": "http://logging:50051",
       "ServiceName": "YourService",
       "HeartbeatIntervalSeconds": 30
     }
   }
   ```

### Using LoggingClient

```csharp
public class YourService
{
    private readonly ILoggingClient _loggingClient;

    public YourService(ILoggingClient loggingClient)
    {
        _loggingClient = loggingClient;
    }

    public async Task DoSomethingAsync()
    {
        try
        {
            // Your business logic
            await _loggingClient.LogAsync(
                level: "Information",
                message: "Operation completed successfully",
                requestPath: "/api/endpoint",
                requestMethod: "POST"
            );
        }
        catch (Exception ex)
        {
            await _loggingClient.LogAsync(
                level: "Error",
                message: "Operation failed",
                exception: ex.ToString(),
                requestPath: "/api/endpoint"
            );
        }
    }
}
```

## Running Locally

### Prerequisites

- .NET 9 SDK
- PostgreSQL 16+

### Setup

1. **Configure Database**:
   Update `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=LoggingService;Username=postgres;Password=postgres"
     }
   }
   ```

2. **Run Migrations**:
   ```bash
   cd Services/LoggingService/LoggingService.Infrastructure
   dotnet ef database update --startup-project ../LoggingService.Api
   ```

3. **Run the Service**:
   ```bash
   cd Services/LoggingService/LoggingService.Api
   dotnet run
   ```

The service will be available at:
- **HTTP API**: `http://localhost:8083`
- **gRPC**: `http://localhost:50051`

## Docker

### Build and Run

```bash
docker compose build logging
docker compose up logging
```

### Ports

- **8083**: HTTP REST API
- **50051**: gRPC endpoint

## Database

### Migrations

Create a new migration:
```bash
cd Services/LoggingService/LoggingService.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../LoggingService.Api
```

Apply migrations:
```bash
dotnet ef database update --startup-project ../LoggingService.Api
```

### Tables

- **ApplicationLogs**: Stores log entries
  - Indexed on: Timestamp, Level, ServiceName, UserId
- **RegisteredServices**: Tracks registered services
  - Indexed on: ServiceName, Status

## REST API (Legacy)

The service also exposes REST endpoints for backward compatibility:

- `POST /api/logs` - Create log entry (anonymous)
- `GET /api/logs` - Query logs (authorized)
- `GET /api/logs/{id}` - Get log by ID
- `GET /api/logs/service/{serviceName}` - Get logs by service
- `GET /api/logs/level/{level}` - Get logs by level

**Note**: New integrations should use gRPC for better performance.

## Service Health

### Monitoring Registered Services

The LoggingService tracks all registered services and their health:

- **Online**: Service is sending heartbeats regularly
- **Offline**: Service has not sent a heartbeat within the timeout period (default: 90 seconds)

Services are automatically marked as offline if they don't send a heartbeat within the configured timeout.

## Troubleshooting

### Service Not Registering

- Check that `LoggingService:GrpcEndpoint` is correctly configured
- Verify LoggingService is running and accessible
- Check network connectivity between services
- Review service logs for registration errors

### Heartbeats Failing

- Verify the heartbeat interval is configured correctly
- Check that the LoggingService gRPC endpoint is accessible
- Ensure the service name matches the registration name

### Logs Not Appearing

- Verify the LoggingClient is registered in DI
- Check that `LogAsync` calls are not throwing exceptions
- Review LoggingService database for stored logs
- Check LoggingService logs for errors

## License

MIT

