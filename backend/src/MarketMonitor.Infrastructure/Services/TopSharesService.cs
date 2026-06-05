using MarketMonitor.Application.Dtos;
using MarketMonitor.Application.Interfaces;
using MarketMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Infrastructure.Services;

/// <summary>
/// Builds the public "Top Shares to Buy" list: take the curated symbol universe,
/// run each through the suggestions engine, and surface the strongest signals.
/// Ordering favours BUY recommendations by confidence, then HOLD, then SELL.
/// </summary>
public sealed class TopSharesService : ITopSharesService
{
    private readonly AppDbContext _db;
    private readonly IMarketDataProvider _marketData;
    private readonly ISuggestionService _suggestions;

    public TopSharesService(
        AppDbContext db,
        IMarketDataProvider marketData,
        ISuggestionService suggestions)
    {
        _db = db;
        _marketData = marketData;
        _suggestions = suggestions;
    }

    public async Task<IReadOnlyList<TopShareDto>> GetTopSharesAsync(int limit = 8, CancellationToken ct = default)
    {
        var symbols = await _db.Symbols
            .Where(s => s.IsCurated)
            .OrderBy(s => s.Ticker)
            .ToListAsync(ct);

        var rows = new List<TopShareDto>(symbols.Count);

        foreach (var symbol in symbols)
        {
            ct.ThrowIfCancellationRequested();

            var quote = await _marketData.GetQuoteAsync(symbol.Ticker, ct);
            var suggestion = await _suggestions.GetSuggestionAsync(symbol.Ticker, ct);

            rows.Add(new TopShareDto
            {
                Symbol = symbol.Ticker,
                Name = symbol.Name,
                Price = quote.Current,
                PercentChange = quote.PercentChange,
                Recommendation = suggestion.Recommendation.ToString().ToUpperInvariant(),
                Confidence = suggestion.Confidence,
                Rationale = suggestion.Summary,
                PriceIsMock = quote.IsMock,
                SignalIsMock = suggestion.BasedOnMockData
            });
        }

        // Rank: BUY first (highest confidence), then HOLD, then SELL.
        return rows
            .OrderByDescending(r => RecommendationRank(r.Recommendation))
            .ThenByDescending(r => r.Confidence)
            .Take(limit)
            .ToList();
    }

    private static int RecommendationRank(string recommendation) => recommendation switch
    {
        "BUY" => 3,
        "HOLD" => 2,
        _ => 1
    };
}
