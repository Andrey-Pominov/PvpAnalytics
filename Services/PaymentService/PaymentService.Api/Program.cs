using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaymentService.Application;
using PaymentService.Infrastructure;
using PvpAnalytics.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// Get JWT options (always needed for configuration, even in test mode)
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
if (jwtOptions == null)
{
    throw new InvalidOperationException("Jwt configuration section is missing.");
}

// Skip JWT validation in test mode (when UseInMemoryDatabase is set)
var useInMemoryDatabaseValue = builder.Configuration["UseInMemoryDatabase"];
var useInMemoryDatabase = !string.IsNullOrWhiteSpace(useInMemoryDatabaseValue) && 
                          (useInMemoryDatabaseValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                           useInMemoryDatabaseValue.Equals("1", StringComparison.OrdinalIgnoreCase));

if (!useInMemoryDatabase)
{
    // Validate JWT signing key with clear error messages (only in non-test mode)
    const string placeholderKey = "DEV_PLACEHOLDER_KEY_MUST_BE_REPLACED";
    if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
    {
        throw new InvalidOperationException(
            "JWT signing key is not configured. " +
            "Please provide a valid signing key using one of the following methods:\n" +
            "  1. User Secrets: dotnet user-secrets set \"Jwt:SigningKey\" \"your-secret-key-here\"\n" +
            "  2. Environment Variable: set Jwt__SigningKey=your-secret-key-here\n" +
            "  3. appsettings.Development.json (local only, never commit real keys to source control)");
    }

    if (jwtOptions.SigningKey.Equals(placeholderKey, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "JWT signing key is still set to the placeholder value. " +
            "You must replace 'DEV_PLACEHOLDER_KEY_MUST_BE_REPLACED' with a real signing key.\n" +
            "Please provide a valid signing key using one of the following methods:\n" +
            "  1. User Secrets: dotnet user-secrets set \"Jwt:SigningKey\" \"your-secret-key-here\"\n" +
            "  2. Environment Variable: set Jwt__SigningKey=your-secret-key-here\n" +
            "  3. appsettings.Development.json (local only, never commit real keys to source control)");
    }
}

// Read CORS origins from configuration
var corsOrigins = builder.Configuration.GetSection($"{CorsOptions.SectionName}:AllowedOrigins").Get<string[]>();
if (corsOrigins == null || corsOrigins.Length == 0)
{
    // In test mode, use a default CORS origin
    if (useInMemoryDatabase)
    {
        corsOrigins = ["http://localhost:3000"];
    }
    else
    {
        throw new InvalidOperationException(
            $"CORS allowed origins are not configured. Set the '{CorsOptions.SectionName}__AllowedOrigins' environment variable or configure '{CorsOptions.SectionName}:AllowedOrigins' in appsettings.json.");
    }
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
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await db.Database.MigrateAsync();
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

namespace PaymentService.Api
{
    public partial class Program;
}
