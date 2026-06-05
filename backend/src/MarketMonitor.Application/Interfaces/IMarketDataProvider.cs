using MarketMonitor.Domain.Market;

namespace MarketMonitor.Application.Interfaces;

/// <summary>
/// Abstraction over a market data source. The default implementation wraps
/// Finnhub, but any provider (Alpha Vantage, Twelve Data, mock) can be dropped
/// in without business logic changes — callers depend only on this interface.
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>Latest quote for a single symbol.</summary>
    Task<Quote> GetQuoteAsync(string symbol, CancellationToken ct = default);

    /// <summary>Latest quotes for several symbols (used by the home page list).</summary>
    Task<IReadOnlyList<Quote>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default);

    /// <summary>Historical OHLC candles for charts and indicators.</summary>
    Task<IReadOnlyList<Candle>> GetCandlesAsync(string symbol, ChartRange range, CancellationToken ct = default);
}
