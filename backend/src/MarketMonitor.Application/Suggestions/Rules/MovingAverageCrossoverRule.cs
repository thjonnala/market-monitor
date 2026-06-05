using MarketMonitor.Domain.Enums;
using MarketMonitor.Domain.Market;

namespace MarketMonitor.Application.Suggestions.Rules;

/// <summary>
/// Classic trend signal: when the short moving average is above the long one
/// the trend is up (lean Buy); below, the trend is down (lean Sell). Weight
/// scales with how far apart the averages are, relative to price.
/// </summary>
public sealed class MovingAverageCrossoverRule : ISignalRule
{
    private readonly int _shortPeriod;
    private readonly int _longPeriod;

    public MovingAverageCrossoverRule(int shortPeriod = 20, int longPeriod = 50)
    {
        _shortPeriod = shortPeriod;
        _longPeriod = longPeriod;
    }

    public string Name => $"SMA {_shortPeriod}/{_longPeriod} crossover";

    public SignalContribution? Evaluate(IReadOnlyList<Candle> candles, Quote quote)
    {
        if (candles.Count < _longPeriod) return null;

        var closes = candles.Select(c => c.Close).ToList();
        var shortMa = Indicators.Sma(closes, _shortPeriod);
        var longMa = Indicators.Sma(closes, _longPeriod);
        if (shortMa is null || longMa is null || longMa == 0m) return null;

        decimal spreadPct = (shortMa.Value - longMa.Value) / longMa.Value * 100m;

        // Deadzone: a near-zero spread means no real trend -> weak Hold.
        if (Math.Abs(spreadPct) < 0.5m)
        {
            return new SignalContribution
            {
                RuleName = Name,
                Lean = Recommendation.Hold,
                Weight = 0.3,
                Rationale =
                    $"{_shortPeriod}-day and {_longPeriod}-day SMAs are converged " +
                    $"({shortMa:F2} vs {longMa:F2}) — no clear trend."
            };
        }

        // Map a spread of ~3% or more to full weight.
        double weight = Math.Min(1.0, Math.Abs((double)spreadPct) / 3.0);

        Recommendation lean = spreadPct >= 0 ? Recommendation.Buy : Recommendation.Sell;
        string direction = spreadPct >= 0 ? "above" : "below";

        return new SignalContribution
        {
            RuleName = Name,
            Lean = lean,
            Weight = weight,
            Rationale =
                $"{_shortPeriod}-day SMA ({shortMa:F2}) is {direction} the " +
                $"{_longPeriod}-day SMA ({longMa:F2}), a {Math.Abs(spreadPct):F1}% " +
                $"{(spreadPct >= 0 ? "bullish" : "bearish")} trend."
        };
    }
}
