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
var serviceEndpoint = GetServiceEndpoint(builder.Configuration, "localhost:8080");
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

static string GetServiceEndpoint(IConfiguration configuration, string defaultEndpoint)
{
    // Returns endpoint in host:port format (without scheme) for consistency
    var urlsValue = configuration["ASPNETCORE_URLS"];
    if (string.IsNullOrWhiteSpace(urlsValue))
    {
        // Ensure defaultEndpoint is in host:port format (strip scheme if present)
        return NormalizeEndpoint(defaultEndpoint);
    }

    var urls = urlsValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var url in urls)
    {
        if (string.IsNullOrWhiteSpace(url))
            continue;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            // Extract host:port (authority) without scheme for consistent format
            return uri.Authority;
        }
    }

    // Return defaultEndpoint in host:port format (strip scheme if present)
    return NormalizeEndpoint(defaultEndpoint);
}

static string NormalizeEndpoint(string endpoint)
{
    // If endpoint contains a scheme (e.g., "http://localhost:8080"), extract host:port
    if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
    {
        return uri.Authority;
    }
    
    // If no scheme, assume it's already in host:port format
    return endpoint;
}

namespace PvpAnalytics.Api
{
    public partial class Program;
}