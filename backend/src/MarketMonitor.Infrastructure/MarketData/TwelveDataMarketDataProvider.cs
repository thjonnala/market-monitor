using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MarketMonitor.Application.Interfaces;
using MarketMonitor.Domain.Market;
using Microsoft.Extensions.Options;

namespace MarketMonitor.Infrastructure.MarketData;

/// <summary>
/// Live data from Twelve Data (https://twelvedata.com). Unlike Finnhub's free
/// tier, Twelve Data's free plan includes historical time series, so charts and
/// the technical signals run on real data.
///
/// Like the Finnhub provider, this only does raw fetching and throws on failure;
/// caching, rate-limit backoff, and mock fallback are layered on by
/// <see cref="ResilientMarketDataProvider"/>.
///
/// Free tier note: ~8 requests/minute. We batch quotes (one call for many
/// symbols) and rely on long-lived candle caching to stay within the limit.
/// </summary>
public sealed class TwelveDataMarketDataProvider : IMarketDataProvider
{
    private readonly HttpClient _http;
    private readonly MarketDataOptions _options;

    public TwelveDataMarketDataProvider(HttpClient http, IOptions<MarketDataOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    private string Key => _options.ApiKey ?? string.Empty;
    private string BaseUrl => string.IsNullOrWhiteSpace(_options.BaseUrl) || _options.BaseUrl.Contains("finnhub")
        ? "https://api.twelvedata.com"
        : _options.BaseUrl;

    public async Task<Quote> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        var url = $"{BaseUrl}/quote?symbol={Uri.EscapeDataString(symbol)}&apikey={Key}";

        using var doc = await GetJsonAsync(url, ct);
        return ParseQuote(symbol, doc.RootElement);
    }

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default)
    {
        var list = symbols.Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList();
        if (list.Count == 0) return Array.Empty<Quote>();
        if (list.Count == 1) return new[] { await GetQuoteAsync(list[0], ct) };

        // Batch: /quote?symbol=A,B,C returns an object keyed by symbol.
        var joined = string.Join(",", list);
        var url = $"{BaseUrl}/quote?symbol={Uri.EscapeDataString(joined)}&apikey={Key}";

        using var doc = await GetJsonAsync(url, ct);
        var root = doc.RootElement;

        var results = new List<Quote>(list.Count);
        foreach (var sym in list)
        {
            // Keyed object when multiple symbols; tolerate a flat object otherwise.
            JsonElement el = root.TryGetProperty(sym, out var keyed) ? keyed : root;
            try
            {
                results.Add(ParseQuote(sym, el));
            }
            catch (MarketDataException)
            {
                // Skip symbols the API couldn't price; the resilient layer fills gaps.
            }
        }

        if (results.Count == 0)
            throw new MarketDataException("No quotes returned for the requested symbols.");
        return results;
    }

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(string symbol, ChartRange range, CancellationToken ct = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        var (interval, outputSize) = RangeToParams(range);

        var url = $"{BaseUrl}/time_series?symbol={Uri.EscapeDataString(symbol)}" +
                  $"&interval={interval}&outputsize={outputSize}&order=ASC&apikey={Key}";

        using var doc = await GetJsonAsync(url, ct);
        var root = doc.RootElement;

        if (!root.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array
            || values.GetArrayLength() == 0)
            throw new MarketDataException($"No time-series data for {symbol}.");

        var candles = new List<Candle>(values.GetArrayLength());
        foreach (var v in values.EnumerateArray())
        {
            candles.Add(new Candle
            {
                Timestamp = ParseDate(v.GetProperty("datetime").GetString()),
                Open = ParseDecimal(v, "open"),
                High = ParseDecimal(v, "high"),
                Low = ParseDecimal(v, "low"),
                Close = ParseDecimal(v, "close"),
                Volume = v.TryGetProperty("volume", out var vol) ? ParseLong(vol) : 0,
                IsMock = false
            });
        }

        // order=ASC should already give oldest-first, but ensure it for the engine.
        candles.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        return candles;
    }

    /// <summary>Fetch + parse JSON, mapping Twelve Data's error envelope to exceptions.</summary>
    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(url, ct);
        if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            throw new RateLimitException("Twelve Data rate limit hit (HTTP 429).");
        resp.EnsureSuccessStatusCode();

        var stream = await resp.Content.ReadAsStreamAsync(ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        // Twelve Data returns HTTP 200 with {"status":"error","code":..,"message":..}.
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("status", out var status)
            && status.GetString() == "error")
        {
            int code = root.TryGetProperty("code", out var c) && c.TryGetInt32(out var ci) ? ci : 0;
            string message = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
            doc.Dispose();
            if (code == 429) throw new RateLimitException($"Twelve Data: {message}");
            throw new MarketDataException($"Twelve Data error {code}: {message}");
        }

        return doc;
    }

    private static Quote ParseQuote(string symbol, JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty("close", out _))
            throw new MarketDataException($"No quote data for {symbol}.");

        decimal close = ParseDecimal(el, "close");
        decimal prevClose = el.TryGetProperty("previous_close", out _) ? ParseDecimal(el, "previous_close") : close;

        return new Quote
        {
            Symbol = symbol,
            Current = close,
            Change = el.TryGetProperty("change", out _) ? ParseDecimal(el, "change") : close - prevClose,
            PercentChange = el.TryGetProperty("percent_change", out _)
                ? ParseDecimal(el, "percent_change")
                : (prevClose == 0 ? 0 : (close - prevClose) / prevClose * 100m),
            High = el.TryGetProperty("high", out _) ? ParseDecimal(el, "high") : close,
            Low = el.TryGetProperty("low", out _) ? ParseDecimal(el, "low") : close,
            Open = el.TryGetProperty("open", out _) ? ParseDecimal(el, "open") : close,
            PreviousClose = prevClose,
            Timestamp = DateTimeOffset.UtcNow,
            IsMock = false
        };
    }

    private static (string Interval, int OutputSize) RangeToParams(ChartRange range) => range switch
    {
        ChartRange.OneDay => ("5min", 78),       // ~ one trading day of 5-min bars
        ChartRange.OneWeek => ("30min", 70),
        ChartRange.OneMonth => ("1day", 30),
        ChartRange.ThreeMonths => ("1day", 90),
        ChartRange.OneYear => ("1week", 52),
        _ => ("1day", 90)
    };

    // Twelve Data sends numbers as strings; parse with invariant culture.
    private static decimal ParseDecimal(JsonElement obj, string prop)
    {
        var s = obj.GetProperty(prop).GetString();
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    private static long ParseLong(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var n)) return n;
        return long.TryParse(el.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var l) ? l : 0;
    }

    private static DateTimeOffset ParseDate(string? s)
    {
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
            return dto;
        return DateTimeOffset.UtcNow;
    }
}
