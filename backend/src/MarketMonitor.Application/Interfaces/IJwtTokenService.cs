namespace MarketMonitor.Application.Interfaces;

/// <summary>Issues signed JWT bearer tokens for authenticated users.</summary>
public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateToken(string userId, string email, string displayName);
}
