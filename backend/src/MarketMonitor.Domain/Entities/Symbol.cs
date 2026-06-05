namespace MarketMonitor.Domain.Entities;

/// <summary>
/// A tradable instrument the app knows about. Seeded with a handful of
/// well-known symbols for local testing and used to power the home page list.
/// </summary>
public sealed class Symbol
{
    public int Id { get; set; }

    /// <summary>Ticker, e.g. "AAPL". Stored uppercase.</summary>
    public required string Ticker { get; set; }

    public required string Name { get; set; }

    public string? Exchange { get; set; }

    /// <summary>Optional sector/category, handy for the curated home list.</summary>
    public string? Sector { get; set; }

    /// <summary>When true the symbol is part of the curated "Top Shares" universe.</summary>
    public bool IsCurated { get; set; }
}
