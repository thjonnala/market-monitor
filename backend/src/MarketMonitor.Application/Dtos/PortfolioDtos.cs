using System.ComponentModel.DataAnnotations;

namespace MarketMonitor.Application.Dtos;

public sealed class WatchlistItemDto
{
    public required string Symbol { get; init; }
    public string? Name { get; init; }
    public decimal Price { get; init; }
    public decimal PercentChange { get; init; }
    public DateTimeOffset AddedAt { get; init; }
}

public sealed class AddWatchlistRequest
{
    [Required, MaxLength(12)]
    public string Symbol { get; set; } = string.Empty;
}

public sealed class HoldingDto
{
    public required string Symbol { get; init; }
    public decimal Quantity { get; init; }
    public decimal AverageCost { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal MarketValue { get; init; }
    public decimal CostBasis { get; init; }
    public decimal UnrealizedPnL { get; init; }
    public decimal UnrealizedPnLPercent { get; init; }
}

public sealed class PortfolioDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public decimal CashBalance { get; init; }
    public decimal InitialCash { get; init; }
    public decimal HoldingsValue { get; init; }
    public decimal TotalValue { get; init; }
    public decimal TotalReturn { get; init; }
    public decimal TotalReturnPercent { get; init; }
    public IReadOnlyList<HoldingDto> Holdings { get; init; } = Array.Empty<HoldingDto>();
}

public sealed class TradeRequest
{
    [Required, MaxLength(12)]
    public string Symbol { get; set; } = string.Empty;

    [Range(1, 1_000_000)]
    public decimal Quantity { get; set; }
}
