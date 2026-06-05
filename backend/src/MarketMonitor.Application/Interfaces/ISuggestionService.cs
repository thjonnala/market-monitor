using MarketMonitor.Application.Suggestions;

namespace MarketMonitor.Application.Interfaces;

/// <summary>
/// Fetches the data a symbol needs and runs it through the suggestions engine.
/// </summary>
public interface ISuggestionService
{
    Task<SuggestionResult> GetSuggestionAsync(string symbol, CancellationToken ct = default);
}
