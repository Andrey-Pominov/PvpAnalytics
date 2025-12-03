using System.Text;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PvpAnalytics.Shared.Security;
using PvpAnalytics.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();  // Built-in OpenAPI support

// Add health checks
builder.Services.AddHealthChecks();

builder.Services.AddInfrastructure(builder.Configuration);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
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

var app = builder.Build();

var skipMigrations = builder.Configuration.GetValue<bool?>("EfMigrations:Skip") ?? false;
if (!skipMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();  // Use built-in OpenAPI endpoint
}

app.UseHttpsRedirection();
app.UseCors(CorsOptions.DefaultPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Add health checks endpoint
app.MapHealthChecks("/health");

// TODO: Add logging client registration once the dependency is resolved
// builder.Services.AddSingleton<ILoggingClient>(sp => {
//     var config = sp.GetRequiredService<IConfiguration>();
//     var logger = sp.GetRequiredService<ILogger<LoggingClient>>();
//     return new LoggingClient(config, logger);
// });

await app.RunAsync();

static string GetServiceEndpoint(IConfiguration configuration, string defaultEndpoint)
{
    var endpoint = configuration["ServiceEndpoints:LoggingService"] ?? defaultEndpoint;
    return endpoint;
}

namespace AuthService.Api
{
    public interface IProgram;
}