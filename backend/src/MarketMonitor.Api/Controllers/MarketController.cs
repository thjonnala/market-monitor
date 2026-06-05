using MarketMonitor.Application.Dtos;
using MarketMonitor.Application.Interfaces;
using MarketMonitor.Domain.Market;
using Microsoft.AspNetCore.Mvc;

namespace MarketMonitor.Api.Controllers;

/// <summary>
/// Public market-data endpoints: the home-page top-shares list, quotes, charts,
/// and per-symbol suggestions. No authentication required.
/// </summary>
[ApiController]
[Route("api/market")]
public sealed class MarketController : ControllerBase
{
    private readonly ITopSharesService _topShares;
    private readonly IMarketDataProvider _marketData;
    private readonly ISuggestionService _suggestions;
    private readonly ISymbolCatalog _catalog;

    public MarketController(
        ITopSharesService topShares,
        IMarketDataProvider marketData,
        ISuggestionService suggestions,
        ISymbolCatalog catalog)
    {
        _topShares = topShares;
        _marketData = marketData;
        _suggestions = suggestions;
        _catalog = catalog;
    }

    /// <summary>Curated "Top Shares to Buy" for the public home page.</summary>
    [HttpGet("top-shares")]
    public async Task<ActionResult<IReadOnlyList<TopShareDto>>> GetTopShares(
        [FromQuery] int limit = 8, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 25);
        return Ok(await _topShares.GetTopSharesAsync(limit, ct));
    }

    /// <summary>Latest quote for a single symbol.</summary>
    [HttpGet("quote/{symbol}")]
    public async Task<ActionResult<QuoteDto>> GetQuote(string symbol, CancellationToken ct = default)
    {
        var quote = await _marketData.GetQuoteAsync(symbol, ct);
        var name = await _catalog.GetNameAsync(symbol, ct);
        return Ok(ToDto(quote, name));
    }

    /// <summary>OHLC candles for the price chart over a selectable range.</summary>
    [HttpGet("candles/{symbol}")]
    public async Task<ActionResult<IReadOnlyList<CandleDto>>> GetCandles(
        string symbol, [FromQuery] string range = "1M", CancellationToken ct = default)
    {
        var candles = await _marketData.GetCandlesAsync(symbol, ParseRange(range), ct);
        var dtos = candles.Select(c => new CandleDto
        {
            Date = c.Timestamp,
            Open = c.Open,
            High = c.High,
            Low = c.Low,
            Close = c.Close,
            Volume = c.Volume,
            IsMock = c.IsMock
        }).ToList();
        return Ok(dtos);
    }

    /// <summary>Explainable BUY/SELL/HOLD suggestion for a symbol.</summary>
    [HttpGet("suggestion/{symbol}")]
    public async Task<ActionResult<SuggestionDto>> GetSuggestion(string symbol, CancellationToken ct = default)
    {
        var s = await _suggestions.GetSuggestionAsync(symbol, ct);
        return Ok(new SuggestionDto
        {
            Symbol = s.Symbol,
            Recommendation = s.Recommendation.ToString().ToUpperInvariant(),
            Confidence = s.Confidence,
            Summary = s.Summary,
            BasedOnMockData = s.BasedOnMockData,
            Signals = s.Signals.Select(sig => new SignalDto
            {
                Rule = sig.RuleName,
                Lean = sig.Lean.ToString().ToUpperInvariant(),
                Weight = Math.Round(sig.Weight, 2),
                Rationale = sig.Rationale
            }).ToList()
        });
    }

    private static QuoteDto ToDto(Quote q, string? name) => new()
    {
        Symbol = q.Symbol,
        Name = name,
        Price = q.Current,
        Change = q.Change,
        PercentChange = q.PercentChange,
        High = q.High,
        Low = q.Low,
        Open = q.Open,
        PreviousClose = q.PreviousClose,
        AsOf = q.Timestamp,
        IsMock = q.IsMock
    };

    private static ChartRange ParseRange(string range) => range.ToUpperInvariant() switch
    {
        "1D" => ChartRange.OneDay,
        "1W" => ChartRange.OneWeek,
        "1M" => ChartRange.OneMonth,
        "3M" => ChartRange.ThreeMonths,
        "1Y" => ChartRange.OneYear,
        _ => ChartRange.OneMonth
    };
}
