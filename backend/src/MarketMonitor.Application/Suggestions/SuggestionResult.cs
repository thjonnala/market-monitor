using MarketMonitor.Domain.Enums;

namespace MarketMonitor.Application.Suggestions;

/// <summary>
/// The final, explainable output of the suggestions engine for one symbol.
/// </summary>
public sealed class SuggestionResult
{
    public required string Symbol { get; init; }

    public Recommendation Recommendation { get; init; }

    /// <summary>Confidence in [0, 1], derived from the agreement of the rules.</summary>
    public double Confidence { get; init; }

    /// <summary>One-line summary suitable for a UI badge tooltip.</summary>
    public required string Summary { get; init; }

    /// <summary>Per-rule breakdown so the user can see *why*.</summary>
    public IReadOnlyList<SignalContribution> Signals { get; init; } = Array.Empty<SignalContribution>();

    /// <summary>True when the underlying data was mock/fallback data.</summary>
    public bool BasedOnMockData { get; init; }
}
