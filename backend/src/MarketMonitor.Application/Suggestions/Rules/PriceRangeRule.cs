using MarketMonitor.Domain.Enums;
using MarketMonitor.Domain.Market;

namespace MarketMonitor.Application.Suggestions.Rules;

/// <summary>
/// Where the latest price sits within its recent trading range, read as a
/// momentum/position signal: in the upper part of the range the stock is pushing
/// highs (lean Buy), in the lower part it is lagging (lean Sell), the middle is
/// Hold. A deadzone ignores ranges that are tiny relative to price so day-to-day
/// noise on a flat stock does not produce a signal.
/// </summary>
public sealed class PriceRangeRule : ISignalRule
{
    private readonly int _window;

    // Ignore ranges narrower than this fraction of price (i.e. effectively flat).
    private const decimal MinRangeFraction = 0.02m;

    public PriceRangeRule(int window = 30)
    {
        _window = window;
    }

    public string Name => $"% of {_window}-day range";

    public SignalContribution? Evaluate(IReadOnlyList<Candle> candles, Quote quote)
    {
        if (candles.Count < _window) return null;

        var window = candles.Skip(candles.Count - _window).Select(c => c.Close).ToList();
        decimal min = window.Min();
        decimal max = window.Max();
        decimal latest = window[^1];

        // Flat / negligible range -> no momentum signal.
        if (latest == 0 || (max - min) / latest < MinRangeFraction)
        {
            return new SignalContribution
            {
                RuleName = Name,
                Lean = Recommendation.Hold,
                Weight = 0.25,
                Rationale = $"Price has been roughly flat over the last {_window} days."
            };
        }

        decimal pct = (latest - min) / (max - min) * 100m;

        if (pct >= 60m)
        {
            return new SignalContribution
            {
                RuleName = Name,
                Lean = Recommendation.Buy,
                Weight = Math.Min(1.0, (double)(pct - 60m) / 40.0 + 0.25),
                Rationale = $"Price is in the upper part of its {_window}-day range ({pct:F0}%), pushing highs."
            };
        }

        if (pct <= 40m)
        {
            return new SignalContribution
            {
                RuleName = Name,
                Lean = Recommendation.Sell,
                Weight = Math.Min(1.0, (double)(40m - pct) / 40.0 + 0.25),
                Rationale = $"Price is in the lower part of its {_window}-day range ({pct:F0}%), lagging."
            };
        }

        return new SignalContribution
        {
            RuleName = Name,
            Lean = Recommendation.Hold,
            Weight = 0.3,
            Rationale = $"Price is mid-range ({pct:F0}% of its {_window}-day range)."
        };
    }
}
