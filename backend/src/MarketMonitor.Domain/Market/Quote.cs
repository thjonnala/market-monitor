namespace MarketMonitor.Domain.Market;

/// <summary>
/// A point-in-time price quote for a symbol. Provider-agnostic shape so the
/// same model works for Finnhub, Alpha Vantage, Twelve Data, or mock data.
/// </summary>
public sealed class Quote
{
    public required string Symbol { get; init; }

    /// <summary>Current / last trade price.</summary>
    public decimal Current { get; init; }

    /// <summary>Absolute change vs previous close.</summary>
    public decimal Change { get; init; }

    /// <summary>Percent change vs previous close.</summary>
    public decimal PercentChange { get; init; }

    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Open { get; init; }
    public decimal PreviousClose { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>True when the quote came from the mock/fallback provider.</summary>
    public bool IsMock { get; init; }
}
