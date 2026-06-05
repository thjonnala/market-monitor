using MarketMonitor.Application.Common;
using MarketMonitor.Application.Dtos;
using MarketMonitor.Application.Interfaces;
using MarketMonitor.Domain.Entities;
using MarketMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Infrastructure.Services;

public sealed class WatchlistService : IWatchlistService
{
    private readonly AppDbContext _db;
    private readonly IMarketDataProvider _marketData;
    private readonly ISymbolCatalog _catalog;

    public WatchlistService(AppDbContext db, IMarketDataProvider marketData, ISymbolCatalog catalog)
    {
        _db = db;
        _marketData = marketData;
        _catalog = catalog;
    }

    public async Task<IReadOnlyList<WatchlistItemDto>> GetWatchlistAsync(string userId, CancellationToken ct = default)
    {
        var items = await _db.WatchlistItems
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync(ct);

        if (items.Count == 0) return Array.Empty<WatchlistItemDto>();

        var tickers = items.Select(i => i.Ticker).ToList();
        var quotes = (await _marketData.GetQuotesAsync(tickers, ct))
            .ToDictionary(q => q.Symbol, StringComparer.OrdinalIgnoreCase);
        var names = await _catalog.GetNamesAsync(tickers, ct);

        return items.Select(i =>
        {
            quotes.TryGetValue(i.Ticker, out var q);
            names.TryGetValue(i.Ticker, out var name);
            return new WatchlistItemDto
            {
                Symbol = i.Ticker,
                Name = name,
                Price = q?.Current ?? 0,
                PercentChange = q?.PercentChange ?? 0,
                AddedAt = i.AddedAt
            };
        }).ToList();
    }

    public async Task<WatchlistItemDto> AddAsync(string userId, string symbol, CancellationToken ct = default)
    {
        symbol = Normalize(symbol);

        bool exists = await _db.WatchlistItems
            .AnyAsync(w => w.UserId == userId && w.Ticker == symbol, ct);
        if (exists)
            throw AppException.Conflict($"{symbol} is already on your watchlist.");

        // Validate the symbol resolves to real (or mock) data before saving.
        var quote = await _marketData.GetQuoteAsync(symbol, ct);

        var item = new WatchlistItem { UserId = userId, Ticker = symbol };
        _db.WatchlistItems.Add(item);
        await _db.SaveChangesAsync(ct);

        var name = await _catalog.GetNameAsync(symbol, ct);
        return new WatchlistItemDto
        {
            Symbol = symbol,
            Name = name,
            Price = quote.Current,
            PercentChange = quote.PercentChange,
            AddedAt = item.AddedAt
        };
    }

    public async Task RemoveAsync(string userId, string symbol, CancellationToken ct = default)
    {
        symbol = Normalize(symbol);
        var item = await _db.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Ticker == symbol, ct)
            ?? throw AppException.NotFound($"{symbol} is not on your watchlist.");

        _db.WatchlistItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }

    private static string Normalize(string symbol)
    {
        symbol = symbol?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(symbol))
            throw new AppException("A symbol is required.");
        return symbol;
    }
}
