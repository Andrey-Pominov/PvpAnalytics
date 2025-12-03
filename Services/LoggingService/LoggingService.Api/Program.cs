using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using LoggingService.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add gRPC services (if using gRPC)
builder.Services.AddGrpc();

// Add health checks
builder.Services.AddHealthChecks();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

var skipMigrations = builder.Configuration.GetValue<bool?>("EfMigrations:Skip") ?? false;
if (!skipMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();  // Use built-in OpenAPI endpoint
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map gRPC services - add your actual gRPC service classes here
// app.MapGrpcService<YourLoggingGrpcService>();

// Add health checks endpoint
app.MapHealthChecks("/health");

// Log startup
app.Logger.LogInformation("Logging service started on configured endpoints");

app.Run();
