using System.Text;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PvpAnalytics.Shared.Security;
using PvpAnalytics.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
_ = builder.Configuration.GetSection(JwtOptions.MyAllowSpecificOrigins).Value;
var jwtOptions = jwtSection.Get<JwtOptions>() ??
                 throw new InvalidOperationException("Jwt configuration section is missing.");
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured. Set the 'Jwt__SigningKey' environment variable or use a secure secret store.");
}

var corsOrigins = builder.Configuration.GetSection($"{CorsOptions.SectionName}:AllowedOrigins").Get<string[]>();
if (corsOrigins == null || corsOrigins.Length == 0)
{
    throw new InvalidOperationException(
        $"CORS allowed origins are not configured. Set the '{CorsOptions.SectionName}__AllowedOrigins' environment variable or configure '{CorsOptions.SectionName}:AllowedOrigins' in appsettings.json.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: CorsOptions.DefaultPolicyName,
        policy => policy
            .WithOrigins(corsOrigins)
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
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
var serviceName = builder.Configuration["LoggingService:ServiceName"] ?? "AuthService";
var serviceEndpoint = builder.Configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault()?.Split("://").LastOrDefault() 
    ?? "http://localhost:8081";
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
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    var maxRetries = 10;
    var delay = TimeSpan.FromSeconds(5);
    for (var i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Attempting to apply database migrations (attempt {Attempt}/{MaxRetries})...", i + 1, maxRetries);
            db.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
            break;
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            logger.LogWarning(ex, "Failed to apply migrations. Retrying in {Delay} seconds...", delay.TotalSeconds);
            Thread.Sleep(delay);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(CorsOptions.DefaultPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace AuthService.Api
{
    public class Program;
}