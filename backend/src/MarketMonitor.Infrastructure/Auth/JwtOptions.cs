namespace MarketMonitor.Infrastructure.Auth;

/// <summary>
/// JWT settings bound from the "Jwt" config section. The signing key must come
/// from user-secrets (local) or an environment variable (prod) — never commit it.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "MarketMonitor";
    public string Audience { get; set; } = "MarketMonitorClient";

    /// <summary>HMAC signing key. Minimum 32 chars for HS256.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 120;
}
