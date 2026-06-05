using MarketMonitor.Domain.Enums;

namespace MarketMonitor.Application.Dtos;

/// <summary>A quote shaped for the frontend.</summary>
public sealed class QuoteDto
{
    public required string Symbol { get; init; }
    public string? Name { get; init; }
    public decimal Price { get; init; }
    public decimal Change { get; init; }
    public decimal PercentChange { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Open { get; init; }
    public decimal PreviousClose { get; init; }
    public DateTimeOffset AsOf { get; init; }
    public bool IsMock { get; init; }
}

/// <summary>A curated "top share" row for the public home page.</summary>
public sealed class TopShareDto
{
    public required string Symbol { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
    public decimal PercentChange { get; init; }
    public required string Recommendation { get; init; } // BUY / SELL / HOLD
    public double Confidence { get; init; }
    public required string Rationale { get; init; }

    /// <summary>True when the price/quote is mock (no live key / unreachable).</summary>
    public bool PriceIsMock { get; init; }

    /// <summary>True when the BUY/SELL signal was computed from mock candle history.</summary>
    public bool SignalIsMock { get; init; }
}

/// <summary>One OHLC point for the price chart.</summary>
public sealed class CandleDto
{
    public DateTimeOffset Date { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public long Volume { get; init; }
    public bool IsMock { get; init; }
}

/// <summary>A single rule's contribution, exposed for the suggestion panel.</summary>
public sealed class SignalDto
{
    public required string Rule { get; init; }
    public required string Lean { get; init; }
    public double Weight { get; init; }
    public required string Rationale { get; init; }
}

public sealed class SuggestionDto
{
    public required string Symbol { get; init; }
    public required string Recommendation { get; init; }
    public double Confidence { get; init; }
    public required string Summary { get; init; }
    public IReadOnlyList<SignalDto> Signals { get; init; } = Array.Empty<SignalDto>();
    public bool BasedOnMockData { get; init; }
}
