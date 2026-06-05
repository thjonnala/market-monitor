namespace MarketMonitor.Domain.Entities;

/// <summary>
/// An open position in a portfolio: how many shares are held and the average
/// cost basis per share. Current value / unrealized P/L are computed at read
/// time using a live quote, so they are not persisted here.
/// </summary>
public sealed class Holding
{
    public int Id { get; set; }

    public int PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }

    public required string Ticker { get; set; }

    /// <summary>Number of shares held. Fractional shares are not supported.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Average price paid per share (cost basis).</summary>
    public decimal AverageCost { get; set; }
}
