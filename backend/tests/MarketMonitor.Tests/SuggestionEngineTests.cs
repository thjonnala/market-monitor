using MarketMonitor.Application.Suggestions;
using MarketMonitor.Application.Suggestions.Rules;
using MarketMonitor.Domain.Enums;
using MarketMonitor.Domain.Market;

namespace MarketMonitor.Tests;

public class SuggestionEngineTests
{
    private static Quote QuoteFor(IReadOnlyList<Candle> candles) => new()
    {
        Symbol = "TEST",
        Current = candles[^1].Close,
        PreviousClose = candles[^2].Close
    };

    /// <summary>Build daily candles from a close-price series.</summary>
    private static List<Candle> Candles(IEnumerable<decimal> closes)
    {
        var start = DateTimeOffset.UtcNow.Date.AddDays(-100);
        return closes.Select((c, i) => new Candle
        {
            Timestamp = start.AddDays(i),
            Open = c, High = c, Low = c, Close = c, Volume = 1_000
        }).ToList();
    }

    [Fact]
    public void StrongUptrend_RecommendsBuy()
    {
        // 80 days steadily rising: short SMA above long SMA, price near range top.
        var closes = Enumerable.Range(1, 80).Select(i => 50m + i).ToList();
        var candles = Candles(closes);

        var result = SuggestionEngine.CreateDefault().Evaluate("TEST", candles, QuoteFor(candles));

        Assert.Equal(Recommendation.Buy, result.Recommendation);
        Assert.True(result.Confidence > 0, "Confidence should be positive for a clear trend.");
        Assert.Contains("BUY", result.Summary);
        Assert.NotEmpty(result.Signals);
    }

    [Fact]
    public void StrongDowntrend_RecommendsSell()
    {
        // 80 days steadily falling.
        var closes = Enumerable.Range(1, 80).Select(i => 200m - i).ToList();
        var candles = Candles(closes);

        var result = SuggestionEngine.CreateDefault().Evaluate("TEST", candles, QuoteFor(candles));

        Assert.Equal(Recommendation.Sell, result.Recommendation);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public void FlatMarket_RecommendsHold()
    {
        // Oscillates tightly around 100 -> no trend, mid-range, neutral RSI.
        var closes = Enumerable.Range(0, 80)
            .Select(i => 100m + (i % 2 == 0 ? 0.5m : -0.5m))
            .ToList();
        var candles = Candles(closes);

        var result = SuggestionEngine.CreateDefault().Evaluate("TEST", candles, QuoteFor(candles));

        Assert.Equal(Recommendation.Hold, result.Recommendation);
    }

    [Fact]
    public void InsufficientData_ReturnsHoldWithZeroConfidence()
    {
        var candles = Candles(new[] { 100m, 101m, 102m }); // far fewer than any rule needs
        var result = SuggestionEngine.CreateDefault().Evaluate("TEST", candles, QuoteFor(candles));

        Assert.Equal(Recommendation.Hold, result.Recommendation);
        Assert.Equal(0, result.Confidence);
        Assert.Contains("Not enough data", result.Summary);
    }

    [Fact]
    public void SummaryRationale_MatchesRecommendationLean()
    {
        var closes = Enumerable.Range(1, 80).Select(i => 50m + i).ToList();
        var candles = Candles(closes);
        var result = SuggestionEngine.CreateDefault().Evaluate("TEST", candles, QuoteFor(candles));

        // The lead rationale should come from a signal that also leans BUY.
        var buySignals = result.Signals.Where(s => s.Lean == Recommendation.Buy).ToList();
        Assert.NotEmpty(buySignals);
        Assert.Contains(buySignals, s => result.Summary.Contains(s.Rationale));
    }

    [Fact]
    public void BasedOnMockData_True_WhenCandlesAreMock_EvenWithLiveQuote()
    {
        var closes = Enumerable.Range(1, 80).Select(i => 50m + i);
        var start = DateTimeOffset.UtcNow.Date.AddDays(-80);
        var candles = closes.Select((c, i) => new Candle
        {
            Timestamp = start.AddDays(i), Open = c, High = c, Low = c, Close = c, Volume = 1, IsMock = true
        }).ToList();
        var liveQuote = new Quote { Symbol = "TEST", Current = candles[^1].Close, PreviousClose = candles[^2].Close, IsMock = false };

        var result = SuggestionEngine.CreateDefault().Evaluate("TEST", candles, liveQuote);

        Assert.True(result.BasedOnMockData, "Mock candles must flag the suggestion as mock-based.");
    }

    [Fact]
    public void Engine_IsExtensible_ViaCustomRuleSet()
    {
        // A single always-Sell rule should drive the decision, proving rules are pluggable.
        var engine = new SuggestionEngine(new[] { new AlwaysSellRule() });
        var candles = Candles(Enumerable.Range(1, 80).Select(i => (decimal)i));

        var result = engine.Evaluate("TEST", candles, QuoteFor(candles));
        Assert.Equal(Recommendation.Sell, result.Recommendation);
    }

    private sealed class AlwaysSellRule : ISignalRule
    {
        public string Name => "AlwaysSell";
        public SignalContribution Evaluate(IReadOnlyList<Candle> candles, Quote quote) => new()
        {
            RuleName = Name,
            Lean = Recommendation.Sell,
            Weight = 1.0,
            Rationale = "Test rule."
        };
    }
}
