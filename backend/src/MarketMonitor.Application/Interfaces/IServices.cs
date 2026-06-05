using MarketMonitor.Application.Dtos;

namespace MarketMonitor.Application.Interfaces;

/// <summary>Builds the public "Top Shares to Buy" home-page list.</summary>
public interface ITopSharesService
{
    Task<IReadOnlyList<TopShareDto>> GetTopSharesAsync(int limit = 8, CancellationToken ct = default);
}

/// <summary>Per-user watchlist management.</summary>
public interface IWatchlistService
{
    Task<IReadOnlyList<WatchlistItemDto>> GetWatchlistAsync(string userId, CancellationToken ct = default);
    Task<WatchlistItemDto> AddAsync(string userId, string symbol, CancellationToken ct = default);
    Task RemoveAsync(string userId, string symbol, CancellationToken ct = default);
}

/// <summary>Virtual portfolio: holdings, buy/sell, and P/L.</summary>
public interface IPortfolioService
{
    Task<PortfolioDto> GetOrCreateAsync(string userId, CancellationToken ct = default);
    Task<PortfolioDto> BuyAsync(string userId, string symbol, decimal quantity, CancellationToken ct = default);
    Task<PortfolioDto> SellAsync(string userId, string symbol, decimal quantity, CancellationToken ct = default);
}

/// <summary>Symbol catalog lookups (names for display, search).</summary>
public interface ISymbolCatalog
{
    Task<string?> GetNameAsync(string ticker, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>> GetNamesAsync(IEnumerable<string> tickers, CancellationToken ct = default);
}
