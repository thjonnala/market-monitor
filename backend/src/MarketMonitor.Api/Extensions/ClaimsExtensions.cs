using System.Security.Claims;

namespace MarketMonitor.Api.Extensions;

public static class ClaimsExtensions
{
    /// <summary>The authenticated user's id, from the NameIdentifier/sub claim.</summary>
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Authenticated user has no id claim.");
}
