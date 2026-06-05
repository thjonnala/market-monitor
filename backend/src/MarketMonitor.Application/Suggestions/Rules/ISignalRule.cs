using MarketMonitor.Domain.Market;

namespace MarketMonitor.Application.Suggestions.Rules;

/// <summary>
/// A single, self-contained trading signal. The engine runs every registered
/// rule and aggregates their votes, so new rules can be added without touching
/// the engine or each other.
/// </summary>
public interface ISignalRule
{
    string Name { get; }

    /// <summary>
    /// Evaluate the rule against historical candles (oldest-to-newest) and the
    /// latest quote. Return null when the rule cannot form an opinion (e.g. not
    /// enough data), in which case it is simply skipped.
    /// </summary>
    SignalContribution? Evaluate(IReadOnlyList<Candle> candles, Quote quote);
}
