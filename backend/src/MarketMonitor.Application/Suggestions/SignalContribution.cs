using MarketMonitor.Domain.Enums;

namespace MarketMonitor.Application.Suggestions;

/// <summary>
/// The vote of a single signal rule. Each rule leans Buy/Sell/Hold with a
/// weight in [0, 1] and an explanation, so the final recommendation is fully
/// traceable back to its inputs.
/// </summary>
public sealed class SignalContribution
{
    public required string RuleName { get; init; }

    public Recommendation Lean { get; init; }

    /// <summary>Strength of this rule's vote, 0 (no opinion) to 1 (strong).</summary>
    public double Weight { get; init; }

    /// <summary>Human-readable reason, e.g. "RSI 72 is overbought".</summary>
    public required string Rationale { get; init; }
}
