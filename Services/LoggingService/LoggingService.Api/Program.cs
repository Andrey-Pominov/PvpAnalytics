using Microsoft.EntityFrameworkCore;
using LoggingService.Infrastructure;
using LoggingService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

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
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapGrpcService<LoggingGrpcService>();

app.MapHealthChecks("/health");

app.Logger.LogInformation("Logging service started on configured endpoints");

app.Run();
