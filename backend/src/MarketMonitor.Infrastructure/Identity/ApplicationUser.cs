using Microsoft.AspNetCore.Identity;

namespace MarketMonitor.Infrastructure.Identity;

/// <summary>
/// Application user backed by ASP.NET Core Identity. Identity hashes passwords;
/// plaintext is never stored. We add a display name on top of the base fields.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
