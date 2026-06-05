using MarketMonitor.Application.Interfaces;
using MarketMonitor.Domain.Market;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketMonitor.Infrastructure.MarketData;

/// <summary>
/// Wraps the live provider with the resilience the free tier needs:
///  - short-TTL in-memory caching of quotes/candles to stay under call limits;
///  - exponential-style backoff after an HTTP 429;
///  - graceful fallback to <see cref="MockMarketDataProvider"/> when the live
///    provider is rate-limited, unreachable, or returns no data.
///
/// Business code depends only on <see cref="IMarketDataProvider"/>, so all of
/// this is transparent to callers.
/// </summary>
public sealed class ResilientMarketDataProvider : IMarketDataProvider
{
    private readonly IMarketDataProvider _live;
    private readonly MockMarketDataProvider _fallback;
    private readonly IMemoryCache _cache;
    private readonly MarketDataOptions _options;
    private readonly ILogger<ResilientMarketDataProvider> _logger;

    // Set when a 429 is seen; while in the future, we skip the live provider.
    private DateTimeOffset _backoffUntil = DateTimeOffset.MinValue;

    public ResilientMarketDataProvider(
        IMarketDataProvider live,
        MockMarketDataProvider fallback,
        IMemoryCache cache,
        IOptions<MarketDataOptions> options,
        ILogger<ResilientMarketDataProvider> logger)
    {
        _live = live;
        _fallback = fallback;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    private bool InBackoff => DateTimeOffset.UtcNow < _backoffUntil;

    public Task<Quote> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        return GetOrAddAsync(
            $"quote:{symbol}",
            TimeSpan.FromSeconds(_options.QuoteCacheSeconds),
            () => _live.GetQuoteAsync(symbol, ct),
            () => _fallback.GetQuoteAsync(symbol, ct));
    }

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default)
    {
        // Reuse the single-quote path so each symbol is cached independently.
        var tasks = symbols.Select(s => GetQuoteAsync(s, ct));
        return await Task.WhenAll(tasks);
    }

    public Task<IReadOnlyList<Candle>> GetCandlesAsync(string symbol, ChartRange range, CancellationToken ct = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        return GetOrAddAsync(
            $"candles:{symbol}:{range}",
            TimeSpan.FromSeconds(_options.CandleCacheSeconds),
            () => _live.GetCandlesAsync(symbol, range, ct),
            () => _fallback.GetCandlesAsync(symbol, range, ct));
    }

    /// <summary>
    /// Cache-aside with live-then-fallback semantics. Mock results are cached
    /// only briefly so we recover quickly once the live provider is healthy.
    /// </summary>
    private async Task<T> GetOrAddAsync<T>(
        string cacheKey,
        TimeSpan ttl,
        Func<Task<T>> liveFetch,
        Func<Task<T>> fallbackFetch)
    {
        if (_cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
            return cached;

        bool fromFallback = false;
        T result;

        if (InBackoff)
        {
            result = await fallbackFetch();
            fromFallback = true;
        }
        else
        {
            try
            {
                result = await liveFetch();
            }
            catch (RateLimitException ex)
            {
                _backoffUntil = DateTimeOffset.UtcNow.AddSeconds(_options.RateLimitBackoffSeconds);
                _logger.LogWarning(ex, "Rate limited; backing off until {Until} and using mock data.", _backoffUntil);
                result = await fallbackFetch();
                fromFallback = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Live market data failed for {Key}; falling back to mock data.", cacheKey);
                result = await fallbackFetch();
                fromFallback = true;
            }
        }

        // Cache live data for the full TTL; cache fallback data briefly.
        var effectiveTtl = fromFallback ? TimeSpan.FromSeconds(Math.Min(15, ttl.TotalSeconds)) : ttl;
        _cache.Set(cacheKey, result, effectiveTtl);
        return result;
    }
}
