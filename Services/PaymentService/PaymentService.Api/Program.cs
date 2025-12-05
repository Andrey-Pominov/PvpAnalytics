using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaymentService.Application;
using PaymentService.Infrastructure;
using PvpAnalytics.Shared.Security;
using PvpAnalytics.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ??
                 throw new InvalidOperationException("Jwt configuration section is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured. Set the 'Jwt__SigningKey' environment variable or use a secure secret store.");
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var useInMemoryDatabase = IsInMemoryDatabaseEnabled(builder.Configuration);
ValidateJwtOptions(jwtOptions, useInMemoryDatabase, builder.Environment);
var corsOrigin = GetCorsOrigins(builder.Configuration, useInMemoryDatabase);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigin)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

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
    var logger = sp.GetRequiredService<ILogger<LoggingClient>>();
    return new LoggingClient(config, logger);
});

var app = builder.Build();

var loggingClient = app.Services.GetRequiredService<ILoggingClient>();
var serviceName = builder.Configuration["LoggingService:ServiceName"] ?? "PaymentService";
var serviceEndpoint = GetServiceEndpoint(builder.Configuration);
const string serviceVersion = "1.0.0";

try
{
    await loggingClient.RegisterServiceAsync(serviceName, serviceEndpoint, serviceVersion);
    var heartbeatInterval = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("LoggingService:HeartbeatIntervalSeconds", 30));
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
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

await app.RunAsync();

static bool IsInMemoryDatabaseEnabled(IConfiguration configuration)
{
    var useInMemoryDatabaseValue = configuration["UseInMemoryDatabase"];
    return !string.IsNullOrWhiteSpace(useInMemoryDatabaseValue) &&
           (useInMemoryDatabaseValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            useInMemoryDatabaseValue.Equals("1", StringComparison.OrdinalIgnoreCase));
}

static void ValidateJwtOptions(JwtOptions jwtOptions, bool useInMemoryDatabase, IHostEnvironment environment)
{
    // In tests we run with an in-memory database and known dummy signing keys.
    // Relax validation when running under the dedicated Testing environment.
    if (useInMemoryDatabase ||
        string.Equals(environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    const string placeholderKey = "DEV_PLACEHOLDER_KEY_MUST_BE_REPLACED";
    const string errorMessage = "Please provide a valid signing key using one of the following methods:\n" +
                                "  1. User Secrets: dotnet user-secrets set \"Jwt:SigningKey\" \"your-secret-key-here\"\n" +
                                "  2. Environment Variable: set Jwt__SigningKey=your-secret-key-here\n" +
                                "  3. appsettings.Development.json (local only, never commit real keys to source control)";

    if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
    {
        throw new InvalidOperationException("JWT signing key is not configured. " + errorMessage);
    }

    if (jwtOptions.SigningKey.Equals(placeholderKey, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "JWT signing key is still set to the placeholder value. " +
            "You must replace 'DEV_PLACEHOLDER_KEY_MUST_BE_REPLACED' with a real signing key.\n" +
            errorMessage);
    }
}

static string GetServiceEndpoint(IConfiguration configuration)
{
    var urlsValue = configuration["ASPNETCORE_URLS"];
    if (string.IsNullOrWhiteSpace(urlsValue))
    {
        return "http://localhost:8082";
    }

    var urls = urlsValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var url in urls)
    {
        if (string.IsNullOrWhiteSpace(url))
            continue;


        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.Authority;
        }
    }
    return "http://localhost:8082";
}

static string[] GetCorsOrigins(IConfiguration configuration, bool useInMemoryDatabase)
{
    var corsOrigins = configuration.GetSection($"{CorsOptions.SectionName}:AllowedOrigins").Get<string[]>();
    if (corsOrigins is { Length: > 0 })
    {
        return corsOrigins;
    }

    if (useInMemoryDatabase)
    {
        return new[] { "http://localhost:3000", "http://localhost:5173" };
    }

    throw new InvalidOperationException(
        $"CORS allowed origins are not configured. Set the '{CorsOptions.SectionName}__AllowedOrigins' environment variable or configure '{CorsOptions.SectionName}:AllowedOrigins' in appsettings.json.");
}

namespace PaymentService.Api
{
    public interface IProgram;
}