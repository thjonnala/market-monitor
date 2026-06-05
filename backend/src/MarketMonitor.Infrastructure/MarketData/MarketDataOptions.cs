namespace MarketMonitor.Infrastructure.MarketData;

/// <summary>
/// Configuration for the market-data layer, bound from the "MarketData" config
/// section. The API key itself comes from user-secrets locally or an env var in
/// production and must never be committed.
/// </summary>
public sealed class MarketDataOptions
{
    public const string SectionName = "MarketData";

    /// <summary>Which provider to use: "Finnhub", "TwelveData", or "Mock".</summary>
    public string Provider { get; set; } = "Finnhub";

    /// <summary>API key for the selected provider. Empty => fall back to mock data.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Base URL. Provider-specific; the active provider applies a sensible default.</summary>
    public string BaseUrl { get; set; } = "https://finnhub.io/api/v1";

    /// <summary>Quote cache lifetime in seconds (free tiers have low limits).</summary>
    public int QuoteCacheSeconds { get; set; } = 30;

    /// <summary>
    /// Candle cache lifetime in seconds. Daily candles only change once per day,
    /// so a long TTL keeps us well within free-tier request limits.
    /// </summary>
    public int CandleCacheSeconds { get; set; } = 1800;

    /// <summary>How long to stop hitting the API after a 429, in seconds.</summary>
    public int RateLimitBackoffSeconds { get; set; } = 60;
}
