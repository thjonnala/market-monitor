using MarketMonitor.Domain.Enums;
using MarketMonitor.Domain.Market;

namespace MarketMonitor.Application.Suggestions.Rules;

/// <summary>
/// RSI as a momentum signal (this engine favours trending, strong-signal stocks):
/// RSI above the mid-line shows bullish momentum (lean Buy), below shows bearish
/// momentum (lean Sell), and a band around 50 is neutral (Hold). Readings past the
/// overbought/oversold thresholds are still directional but their weight is damped
/// — a stretched stock is a weaker conviction than one with healthy momentum — and
/// the rationale flags the caution.
/// </summary>
public sealed class RsiRule : ISignalRule
{
    private readonly int _period;
    private readonly decimal _oversold;
    private readonly decimal _overbought;

    // Half-width of the neutral band around 50.
    private const decimal NeutralHalfBand = 5m;

    public RsiRule(int period = 14, decimal oversold = 30m, decimal overbought = 70m)
    {
        _period = period;
        _oversold = oversold;
        _overbought = overbought;
    }

    public string Name => $"RSI({_period})";

    public SignalContribution? Evaluate(IReadOnlyList<Candle> candles, Quote quote)
    {
        var closes = candles.Select(c => c.Close).ToList();
        var rsi = Indicators.Rsi(closes, _period);
        if (rsi is null) return null;

        decimal value = rsi.Value;

        // Neutral band around the mid-line -> weak Hold.
        if (Math.Abs(value - 50m) <= NeutralHalfBand)
        {
            return new SignalContribution
            {
                RuleName = Name,
                Lean = Recommendation.Hold,
                Weight = 0.3,
                Rationale = $"RSI is {value:F0}, near the neutral mid-line."
            };
        }

        bool bullish = value > 50m;
        // Strength grows from the neutral edge toward the overbought/oversold line.
        decimal edge = bullish ? 50m + NeutralHalfBand : 50m - NeutralHalfBand;
        decimal threshold = bullish ? _overbought : _oversold;
        double span = (double)Math.Abs(threshold - edge);
        double strength = span <= 0 ? 1.0 : Math.Min(1.0, (double)Math.Abs(value - edge) / span);

        bool stretched = bullish ? value >= _overbought : value <= _oversold;
        double weight = stretched ? strength * 0.5 : strength; // damp extremes
        weight = Math.Max(0.2, weight);

        string note = stretched
            ? bullish ? " (overbought — momentum may be stretched)" : " (oversold — momentum may be stretched)"
            : string.Empty;

        return new SignalContribution
        {
            RuleName = Name,
            Lean = bullish ? Recommendation.Buy : Recommendation.Sell,
            Weight = weight,
            Rationale = $"RSI is {value:F0}, showing {(bullish ? "bullish" : "bearish")} momentum{note}."
        };
    }
}
