using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using LoggingService.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddGrpc();

builder.Services.AddHealthChecks();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Database migration
var skipMigrations = builder.Configuration.GetValue<bool?>("EfMigrations:Skip") ?? false;
if (!skipMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); 
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.MapControllers();

// Map gRPC services - add your actual gRPC service classes here
// TODO: Map gRPC services once proto definitions and generated classes are available
// app.MapGrpcService<LoggingGrpcService>();

app.MapHealthChecks("/health");

app.Logger.LogInformation("Logging service started on configured endpoints");

app.Run();
