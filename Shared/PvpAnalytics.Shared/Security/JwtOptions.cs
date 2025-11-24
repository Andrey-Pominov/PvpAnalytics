namespace PvpAnalytics.Shared.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public string SigningKey { get; init; } = null!;
    public int AccessTokenMinutes { get; init; } = 60;
    public int RefreshTokenDays { get; init; } = 7;
}