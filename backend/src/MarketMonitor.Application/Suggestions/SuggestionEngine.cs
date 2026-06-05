using MarketMonitor.Application.Suggestions.Rules;
using MarketMonitor.Domain.Enums;
using MarketMonitor.Domain.Market;

namespace MarketMonitor.Application.Suggestions;

/// <summary>
/// Aggregates the votes of a set of <see cref="ISignalRule"/> into a single,
/// explainable BUY/SELL/HOLD recommendation with a confidence score.
///
/// The engine itself holds no market knowledge — all logic lives in the rules,
/// so behaviour is changed by adding/removing rules rather than editing this.
/// </summary>
public sealed class SuggestionEngine
{
    private readonly IReadOnlyList<ISignalRule> _rules;

    /// <summary>Net score in [-1,1] must clear this band to be Buy/Sell, else Hold.</summary>
    private const double DecisionThreshold = 0.15;

    public SuggestionEngine(IEnumerable<ISignalRule> rules)
    {
        _rules = rules.ToList();
    }

    /// <summary>The default rule set used in production.</summary>
    public static SuggestionEngine CreateDefault() => new(new ISignalRule[]
    {
        new MovingAverageCrossoverRule(),
        new RsiRule(),
        new PriceRangeRule()
    });

    public SuggestionResult Evaluate(string symbol, IReadOnlyList<Candle> candles, Quote quote)
    {
        // The signal is only as "live" as its weakest input: a real-time quote is
        // not enough if the indicators were computed from mock/fallback candles.
        bool basedOnMock = quote.IsMock || candles.Any(c => c.IsMock);

        var contributions = new List<SignalContribution>();
        foreach (var rule in _rules)
        {
            var c = rule.Evaluate(candles, quote);
            if (c is not null && c.Weight > 0) contributions.Add(c);
        }

        if (contributions.Count == 0)
        {
            return new SuggestionResult
            {
                Symbol = symbol,
                Recommendation = Recommendation.Hold,
                Confidence = 0,
                Summary = "Not enough data to form a recommendation.",
                Signals = contributions,
                BasedOnMockData = basedOnMock
            };
        }

        double totalWeight = contributions.Sum(c => c.Weight);
        double buyWeight = contributions.Where(c => c.Lean == Recommendation.Buy).Sum(c => c.Weight);
        double sellWeight = contributions.Where(c => c.Lean == Recommendation.Sell).Sum(c => c.Weight);
        double holdWeight = contributions.Where(c => c.Lean == Recommendation.Hold).Sum(c => c.Weight);

        double net = (buyWeight - sellWeight) / totalWeight; // -1 (all sell) .. +1 (all buy)

        Recommendation recommendation;
        double confidence;

        if (net >= DecisionThreshold)
        {
            recommendation = Recommendation.Buy;
            confidence = buyWeight / totalWeight;
        }
        else if (net <= -DecisionThreshold)
        {
            recommendation = Recommendation.Sell;
            confidence = sellWeight / totalWeight;
        }
        else
        {
            recommendation = Recommendation.Hold;
            // Confident hold when rules agree on hold or simply cancel out near zero.
            confidence = Math.Max(holdWeight / totalWeight, 1.0 - Math.Abs(net));
        }

        confidence = Math.Clamp(confidence, 0.0, 1.0);

        return new SuggestionResult
        {
            Symbol = symbol,
            Recommendation = recommendation,
            Confidence = Math.Round(confidence, 2),
            Summary = BuildSummary(recommendation, confidence, contributions),
            Signals = contributions,
            BasedOnMockData = basedOnMock
        };
    }

    private static string BuildSummary(
        Recommendation rec, double confidence, IReadOnlyList<SignalContribution> contributions)
    {
        string verb = rec switch
        {
            Recommendation.Buy => "BUY",
            Recommendation.Sell => "SELL",
            _ => "HOLD"
        };

        // Lead with the strongest signal that agrees with the final call, so the
        // headline rationale matches the recommendation; fall back to the strongest.
        var top = contributions
            .Where(c => c.Lean == rec)
            .OrderByDescending(c => c.Weight)
            .FirstOrDefault()
            ?? contributions.OrderByDescending(c => c.Weight).First();

        return $"{verb} ({confidence:P0} confidence). {top.Rationale}";
    }
}
