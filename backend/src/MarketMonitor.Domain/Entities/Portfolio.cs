namespace MarketMonitor.Domain.Entities;

/// <summary>
/// A user's virtual portfolio. Holds virtual cash and a set of holdings.
/// No real money or real trades are ever involved.
/// </summary>
public sealed class Portfolio
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public required string Name { get; set; }

    /// <summary>Virtual cash available to "buy" shares.</summary>
    public decimal CashBalance { get; set; }

    /// <summary>Starting virtual cash, retained so total return can be shown.</summary>
    public decimal InitialCash { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Holding> Holdings { get; set; } = new();
    public List<Trade> Trades { get; set; } = new();
}
