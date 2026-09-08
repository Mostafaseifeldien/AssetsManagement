namespace AssetsManagement.Infrastructure;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string SigningKey { get; init; } = "";
    public int ExpiryMinutes { get; init; } = 60;
}
