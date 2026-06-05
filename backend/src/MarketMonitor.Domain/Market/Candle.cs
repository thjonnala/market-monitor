namespace MarketMonitor.Domain.Market;

/// <summary>
/// A single OHLCV candle used for charts and for the suggestions engine's
/// technical indicators (SMA/EMA/RSI).
/// </summary>
public sealed class Candle
{
    public DateTimeOffset Timestamp { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public long Volume { get; init; }

    /// <summary>True when this candle came from the mock/fallback provider.</summary>
    public bool IsMock { get; init; }
}
