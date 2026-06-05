namespace MarketMonitor.Domain.Entities;

/// <summary>
/// A symbol a user has pinned to their personal watchlist.
/// </summary>
public sealed class WatchlistItem
{
    public int Id { get; set; }

    /// <summary>FK to the Identity user (string key).</summary>
    public required string UserId { get; set; }

    public required string Ticker { get; set; }

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
