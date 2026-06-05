using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MarketMonitor.Application.Interfaces;
using MarketMonitor.Domain.Market;
using Microsoft.Extensions.Options;

namespace MarketMonitor.Infrastructure.MarketData;

/// <summary>
/// Live data from Finnhub (https://finnhub.io). Implements only raw fetching and
/// throws on failure; caching, rate-limit backoff, and mock fallback are added
/// by <see cref="ResilientMarketDataProvider"/> so this class stays focused.
///
/// Note: on Finnhub's free tier the /stock/candle endpoint is premium-only and
/// returns 403. When that happens we throw, and the resilience layer supplies
/// mock candles so charts and indicators still work in development.
/// </summary>
public sealed class FinnhubMarketDataProvider : IMarketDataProvider
{
    private readonly HttpClient _http;
    private readonly MarketDataOptions _options;

    public FinnhubMarketDataProvider(HttpClient http, IOptions<MarketDataOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    private string Key => _options.ApiKey ?? string.Empty;

    public async Task<Quote> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        var url = $"{_options.BaseUrl}/quote?symbol={Uri.EscapeDataString(symbol)}&token={Key}";

        using var resp = await _http.GetAsync(url, ct);
        ThrowIfRateLimited(resp);
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<FinnhubQuote>(cancellationToken: ct)
                  ?? throw new MarketDataException($"Empty quote response for {symbol}.");

        // Finnhub returns all-zero for an unknown symbol.
        if (dto.Current == 0 && dto.PreviousClose == 0)
            throw new MarketDataException($"No quote data for {symbol}.");

        return new Quote
        {
            Symbol = symbol,
            Current = dto.Current,
            Change = dto.Change,
            PercentChange = dto.PercentChange,
            High = dto.High,
            Low = dto.Low,
            Open = dto.Open,
            PreviousClose = dto.PreviousClose,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(dto.Timestamp > 0 ? dto.Timestamp : DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            IsMock = false
        };
    }

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default)
    {
        // Finnhub has no batch quote endpoint on the free tier; fetch sequentially
        // to stay friendly to the rate limit. The resilience layer caches each one.
        var results = new List<Quote>();
        foreach (var s in symbols)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await GetQuoteAsync(s, ct));
        }
        return results;
    }

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(string symbol, ChartRange range, CancellationToken ct = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        var (resolution, from, to) = RangeToParams(range);

        var url = $"{_options.BaseUrl}/stock/candle?symbol={Uri.EscapeDataString(symbol)}" +
                  $"&resolution={resolution}&from={from}&to={to}&token={Key}";

        using var resp = await _http.GetAsync(url, ct);
        ThrowIfRateLimited(resp);

        // Free tier: candles are premium -> 403. Throw so we fall back to mock.
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<FinnhubCandles>(cancellationToken: ct);
        if (dto is null || dto.Status != "ok" || dto.Close is null || dto.Close.Length == 0)
            throw new MarketDataException($"No candle data for {symbol}.");

        var candles = new List<Candle>(dto.Close.Length);
        for (int i = 0; i < dto.Close.Length; i++)
        {
            candles.Add(new Candle
            {
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(dto.Timestamps![i]),
                Open = dto.Open![i],
                High = dto.High![i],
                Low = dto.Low![i],
                Close = dto.Close[i],
                Volume = dto.Volume is not null ? (long)dto.Volume[i] : 0
            });
        }
        return candles;
    }

    private static (string Resolution, long From, long To) RangeToParams(ChartRange range)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return range switch
        {
            ChartRange.OneDay => ("5", now.AddDays(-1).ToUnixTimeSeconds(), now.ToUnixTimeSeconds()),
            ChartRange.OneWeek => ("30", now.AddDays(-7).ToUnixTimeSeconds(), now.ToUnixTimeSeconds()),
            ChartRange.OneMonth => ("D", now.AddMonths(-1).ToUnixTimeSeconds(), now.ToUnixTimeSeconds()),
            ChartRange.ThreeMonths => ("D", now.AddMonths(-3).ToUnixTimeSeconds(), now.ToUnixTimeSeconds()),
            ChartRange.OneYear => ("W", now.AddYears(-1).ToUnixTimeSeconds(), now.ToUnixTimeSeconds()),
            _ => ("D", now.AddMonths(-3).ToUnixTimeSeconds(), now.ToUnixTimeSeconds())
        };
    }

    private static void ThrowIfRateLimited(HttpResponseMessage resp)
    {
        if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            throw new RateLimitException("Finnhub rate limit hit (HTTP 429).");
    }

    private sealed class FinnhubQuote
    {
        [JsonPropertyName("c")] public decimal Current { get; set; }
        [JsonPropertyName("d")] public decimal Change { get; set; }
        [JsonPropertyName("dp")] public decimal PercentChange { get; set; }
        [JsonPropertyName("h")] public decimal High { get; set; }
        [JsonPropertyName("l")] public decimal Low { get; set; }
        [JsonPropertyName("o")] public decimal Open { get; set; }
        [JsonPropertyName("pc")] public decimal PreviousClose { get; set; }
        [JsonPropertyName("t")] public long Timestamp { get; set; }
    }

    private sealed class FinnhubCandles
    {
        [JsonPropertyName("s")] public string? Status { get; set; }
        [JsonPropertyName("c")] public decimal[]? Close { get; set; }
        [JsonPropertyName("o")] public decimal[]? Open { get; set; }
        [JsonPropertyName("h")] public decimal[]? High { get; set; }
        [JsonPropertyName("l")] public decimal[]? Low { get; set; }
        [JsonPropertyName("v")] public decimal[]? Volume { get; set; }
        [JsonPropertyName("t")] public long[]? Timestamps { get; set; }
    }
}

/// <summary>A recoverable market-data fetch error (bad/empty data).</summary>
public class MarketDataException : Exception
{
    public MarketDataException(string message) : base(message) { }
}

/// <summary>Raised when the live provider reports HTTP 429.</summary>
public sealed class RateLimitException : MarketDataException
{
    public RateLimitException(string message) : base(message) { }
}
