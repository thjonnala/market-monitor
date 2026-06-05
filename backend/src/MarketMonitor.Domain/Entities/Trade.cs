using MarketMonitor.Domain.Enums;

namespace MarketMonitor.Domain.Entities;

/// <summary>
/// A virtual buy/sell transaction recorded against a portfolio. Provides an
/// audit trail and lets us reconstruct holdings and realized P/L.
/// </summary>
public sealed class Trade
{
    public int Id { get; set; }

    public int PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }

    public required string Ticker { get; set; }

    public TradeSide Side { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Execution price per share at the time of the virtual trade.</summary>
    public decimal Price { get; set; }

    public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;
}
