using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PvpAnalytics.Shared.Security;
using PvpAnalytics.Shared.Services;
using PvpAnalytics.Application;
using PvpAnalytics.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

builder.Services.AddOpenApi();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("Jwt configuration section is missing.");
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException("JWT signing key is not configured. Set 'Jwt__SigningKey' via environment variable or secret manager.");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<ILoggingClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<PvpAnalytics.Shared.Services.LoggingClient>>();
    return new PvpAnalytics.Shared.Services.LoggingClient(config, logger);
});

var app = builder.Build();

var loggingClient = app.Services.GetRequiredService<ILoggingClient>();
var serviceName = builder.Configuration["LoggingService:ServiceName"] ?? "PvpAnalytics";
var serviceEndpoint = builder.Configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault()?.Split("://").LastOrDefault() 
    ?? "http://localhost:8080";
var serviceVersion = "1.0.0";

try
{
    await loggingClient.RegisterServiceAsync(serviceName, serviceEndpoint, serviceVersion);
    var heartbeatInterval = TimeSpan.FromSeconds(
        builder.Configuration.GetValue<int>("LoggingService:HeartbeatIntervalSeconds", 30));
    loggingClient.StartHeartbeat(serviceName, heartbeatInterval);
    app.Logger.LogInformation("Registered with LoggingService and started heartbeat");
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to register with LoggingService, continuing without centralized logging");
}

var skipMigrations = builder.Configuration.GetValue<bool?>("EfMigrations:Skip") ?? false;
if (!skipMigrations)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<PvpAnalyticsDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();

namespace PvpAnalytics.Api
{
    public partial class Program;
}