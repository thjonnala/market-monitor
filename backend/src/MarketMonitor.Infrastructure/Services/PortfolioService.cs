using MarketMonitor.Application.Common;
using MarketMonitor.Application.Dtos;
using MarketMonitor.Application.Interfaces;
using MarketMonitor.Domain.Entities;
using MarketMonitor.Domain.Enums;
using MarketMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Infrastructure.Services;

/// <summary>
/// Virtual portfolio: one per user, seeded with virtual cash. "Buying" debits
/// cash and updates the weighted-average cost basis; "selling" credits cash and
/// reduces the position. All prices come from the live/mock quote at trade time.
/// No real money or real orders are involved.
/// </summary>
public sealed class PortfolioService : IPortfolioService
{
    private const decimal StartingCash = 100_000m;

    private readonly AppDbContext _db;
    private readonly IMarketDataProvider _marketData;

    public PortfolioService(AppDbContext db, IMarketDataProvider marketData)
    {
        _db = db;
        _marketData = marketData;
    }

    public async Task<PortfolioDto> GetOrCreateAsync(string userId, CancellationToken ct = default)
    {
        var portfolio = await LoadAsync(userId, ct) ?? await CreateAsync(userId, ct);
        return await ToDtoAsync(portfolio, ct);
    }

    public async Task<PortfolioDto> BuyAsync(string userId, string symbol, decimal quantity, CancellationToken ct = default)
    {
        symbol = Normalize(symbol, quantity);
        var portfolio = await LoadAsync(userId, ct) ?? await CreateAsync(userId, ct);

        var quote = await _marketData.GetQuoteAsync(symbol, ct);
        decimal price = quote.Current;
        decimal cost = price * quantity;

        if (cost > portfolio.CashBalance)
            throw new AppException(
                $"Insufficient virtual cash: need {cost:C} but only {portfolio.CashBalance:C} available.");

        var holding = portfolio.Holdings.FirstOrDefault(h => h.Ticker == symbol);
        if (holding is null)
        {
            holding = new Holding { Ticker = symbol, Quantity = quantity, AverageCost = price, PortfolioId = portfolio.Id };
            portfolio.Holdings.Add(holding);
        }
        else
        {
            // Weighted-average cost basis.
            decimal totalCost = holding.AverageCost * holding.Quantity + cost;
            holding.Quantity += quantity;
            holding.AverageCost = totalCost / holding.Quantity;
        }

        portfolio.CashBalance -= cost;
        portfolio.Trades.Add(new Trade
        {
            Ticker = symbol, Side = TradeSide.Buy, Quantity = quantity, Price = price, PortfolioId = portfolio.Id
        });

        await _db.SaveChangesAsync(ct);
        return await ToDtoAsync(portfolio, ct);
    }

    public async Task<PortfolioDto> SellAsync(string userId, string symbol, decimal quantity, CancellationToken ct = default)
    {
        symbol = Normalize(symbol, quantity);
        var portfolio = await LoadAsync(userId, ct)
            ?? throw AppException.NotFound("You don't have a portfolio yet.");

        var holding = portfolio.Holdings.FirstOrDefault(h => h.Ticker == symbol)
            ?? throw new AppException($"You don't hold any {symbol}.");

        if (quantity > holding.Quantity)
            throw new AppException($"You only hold {holding.Quantity} share(s) of {symbol}.");

        var quote = await _marketData.GetQuoteAsync(symbol, ct);
        decimal price = quote.Current;
        decimal proceeds = price * quantity;

        holding.Quantity -= quantity;
        if (holding.Quantity == 0)
            portfolio.Holdings.Remove(holding);

        portfolio.CashBalance += proceeds;
        portfolio.Trades.Add(new Trade
        {
            Ticker = symbol, Side = TradeSide.Sell, Quantity = quantity, Price = price, PortfolioId = portfolio.Id
        });

        await _db.SaveChangesAsync(ct);
        return await ToDtoAsync(portfolio, ct);
    }

    private Task<Portfolio?> LoadAsync(string userId, CancellationToken ct) =>
        _db.Portfolios
            .Include(p => p.Holdings)
            .Include(p => p.Trades)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    private async Task<Portfolio> CreateAsync(string userId, CancellationToken ct)
    {
        var portfolio = new Portfolio
        {
            UserId = userId,
            Name = "My Portfolio",
            CashBalance = StartingCash,
            InitialCash = StartingCash
        };
        _db.Portfolios.Add(portfolio);
        await _db.SaveChangesAsync(ct);
        return portfolio;
    }

    private async Task<PortfolioDto> ToDtoAsync(Portfolio portfolio, CancellationToken ct)
    {
        var holdingDtos = new List<HoldingDto>(portfolio.Holdings.Count);
        decimal holdingsValue = 0m;

        if (portfolio.Holdings.Count > 0)
        {
            var quotes = (await _marketData.GetQuotesAsync(portfolio.Holdings.Select(h => h.Ticker), ct))
                .ToDictionary(q => q.Symbol, StringComparer.OrdinalIgnoreCase);

            foreach (var h in portfolio.Holdings.OrderBy(h => h.Ticker))
            {
                quotes.TryGetValue(h.Ticker, out var q);
                decimal current = q?.Current ?? h.AverageCost;
                decimal marketValue = current * h.Quantity;
                decimal costBasis = h.AverageCost * h.Quantity;
                decimal pnl = marketValue - costBasis;

                holdingsValue += marketValue;
                holdingDtos.Add(new HoldingDto
                {
                    Symbol = h.Ticker,
                    Quantity = h.Quantity,
                    AverageCost = decimal.Round(h.AverageCost, 2),
                    CurrentPrice = decimal.Round(current, 2),
                    MarketValue = decimal.Round(marketValue, 2),
                    CostBasis = decimal.Round(costBasis, 2),
                    UnrealizedPnL = decimal.Round(pnl, 2),
                    UnrealizedPnLPercent = costBasis == 0 ? 0 : decimal.Round(pnl / costBasis * 100m, 2)
                });
            }
        }

        decimal totalValue = portfolio.CashBalance + holdingsValue;
        decimal totalReturn = totalValue - portfolio.InitialCash;

        return new PortfolioDto
        {
            Id = portfolio.Id,
            Name = portfolio.Name,
            CashBalance = decimal.Round(portfolio.CashBalance, 2),
            InitialCash = portfolio.InitialCash,
            HoldingsValue = decimal.Round(holdingsValue, 2),
            TotalValue = decimal.Round(totalValue, 2),
            TotalReturn = decimal.Round(totalReturn, 2),
            TotalReturnPercent = portfolio.InitialCash == 0 ? 0 : decimal.Round(totalReturn / portfolio.InitialCash * 100m, 2),
            Holdings = holdingDtos
        };
    }

    private static string Normalize(string symbol, decimal quantity)
    {
        if (quantity <= 0)
            throw new AppException("Quantity must be greater than zero.");
        symbol = symbol?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(symbol))
            throw new AppException("A symbol is required.");
        return symbol;
    }
}
