using MarketMonitor.Application.Interfaces;
using MarketMonitor.Domain.Market;

namespace MarketMonitor.Infrastructure.MarketData;

/// <summary>
/// Deterministic, offline market data so the app runs with no API key and as a
/// fallback when the live provider is unreachable or rate-limited. Prices are
/// generated from a per-symbol seed so charts look plausible and stable across
/// requests within a day.
/// </summary>
public sealed class MockMarketDataProvider : IMarketDataProvider
{
    // A stable base price per known symbol; unknown symbols derive one from a hash.
    private static readonly Dictionary<string, decimal> BasePrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AAPL"] = 195m,
        ["MSFT"] = 415m,
        ["GOOGL"] = 168m,
        ["AMZN"] = 185m,
        ["NVDA"] = 122m,
        ["META"] = 505m,
        ["TSLA"] = 245m,
        ["JPM"] = 205m,
        ["AMD"] = 165m,
        ["NFLX"] = 620m,
        ["BAC"] = 40m,
        ["KO"] = 62m,
        ["V"] = 280m,
        ["DIS"] = 102m,
    };

    public Task<Quote> GetQuoteAsync(string symbol, CancellationToken ct = default)
        => Task.FromResult(BuildQuote(symbol));

    public Task<IReadOnlyList<Quote>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default)
    {
        IReadOnlyList<Quote> quotes = symbols
            .Select(BuildQuote)
            .ToList();
        return Task.FromResult(quotes);
    }

    public Task<IReadOnlyList<Candle>> GetCandlesAsync(string symbol, ChartRange range, CancellationToken ct = default)
    {
        int days = range switch
        {
            ChartRange.OneDay => 2,
            ChartRange.OneWeek => 7,
            ChartRange.OneMonth => 30,
            ChartRange.ThreeMonths => 90,
            ChartRange.OneYear => 365,
            _ => 90
        };

        var candles = BuildCandles(symbol, days);
        return Task.FromResult<IReadOnlyList<Candle>>(candles);
    }

    private static decimal BasePrice(string symbol)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        if (BasePrices.TryGetValue(symbol, out var p)) return p;

        // Stable pseudo-price in [20, 520) derived from the symbol text.
        int hash = Math.Abs(StableHash(symbol));
        return 20m + hash % 500;
    }

    private static Quote BuildQuote(string symbol)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        var candles = BuildCandles(symbol, 5);
        var last = candles[^1];
        var prev = candles[^2];

        decimal change = last.Close - prev.Close;
        decimal pct = prev.Close == 0 ? 0 : change / prev.Close * 100m;

        return new Quote
        {
            Symbol = symbol,
            Current = last.Close,
            Change = decimal.Round(change, 2),
            PercentChange = decimal.Round(pct, 2),
            High = last.High,
            Low = last.Low,
            Open = last.Open,
            PreviousClose = prev.Close,
            Timestamp = DateTimeOffset.UtcNow,
            IsMock = true
        };
    }

    private static List<Candle> BuildCandles(string symbol, int days)
    {
        decimal basePrice = BasePrice(symbol);
        // Per-symbol, per-day deterministic RNG so values are stable within a day.
        int seed = StableHash(symbol) ^ DateTime.UtcNow.DayOfYear;
        var rng = new Random(seed);

        var candles = new List<Candle>(days);
        decimal price = basePrice;
        DateTimeOffset start = DateTimeOffset.UtcNow.Date.AddDays(-days + 1);

        for (int i = 0; i < days; i++)
        {
            // Gentle random walk with mild mean reversion toward basePrice.
            double drift = (rng.NextDouble() - 0.48) * 0.04; // ~±4% daily
            double reversion = (double)((basePrice - price) / basePrice) * 0.05;
            price *= (decimal)(1 + drift + reversion);
            if (price < 1m) price = 1m;

            decimal open = price * (decimal)(1 + (rng.NextDouble() - 0.5) * 0.01);
            decimal high = Math.Max(open, price) * (decimal)(1 + rng.NextDouble() * 0.012);
            decimal low = Math.Min(open, price) * (decimal)(1 - rng.NextDouble() * 0.012);

            candles.Add(new Candle
            {
                Timestamp = start.AddDays(i),
                Open = decimal.Round(open, 2),
                High = decimal.Round(high, 2),
                Low = decimal.Round(low, 2),
                Close = decimal.Round(price, 2),
                Volume = 1_000_000 + rng.Next(0, 9_000_000),
                IsMock = true
            });
        }

        return candles;
    }

    // Deterministic across runs/platforms (string.GetHashCode is randomized per process).
    private static int StableHash(string s)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in s) hash = hash * 31 + c;
            return hash;
        }
    }
}
