using MarketMonitor.Application.Interfaces;
using MarketMonitor.Application.Suggestions;
using MarketMonitor.Domain.Market;

namespace MarketMonitor.Application.Services;

/// <summary>
/// Orchestrates a suggestion: pull a quote + recent candles for the symbol and
/// run them through the (stateless) <see cref="SuggestionEngine"/>.
/// </summary>
public sealed class SuggestionService : ISuggestionService
{
    private readonly IMarketDataProvider _marketData;
    private readonly SuggestionEngine _engine;

    public SuggestionService(IMarketDataProvider marketData, SuggestionEngine engine)
    {
        _marketData = marketData;
        _engine = engine;
    }

    public async Task<SuggestionResult> GetSuggestionAsync(string symbol, CancellationToken ct = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();

        // Three months of daily candles gives the 50-day SMA and RSI room to work.
        var candlesTask = _marketData.GetCandlesAsync(symbol, ChartRange.ThreeMonths, ct);
        var quoteTask = _marketData.GetQuoteAsync(symbol, ct);
        await Task.WhenAll(candlesTask, quoteTask);

        return _engine.Evaluate(symbol, await candlesTask, await quoteTask);
    }
}
