using MarketMonitor.Application.Interfaces;
using MarketMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Infrastructure.Services;

/// <summary>Looks up display names for tickers from the seeded symbol catalog.</summary>
public sealed class SymbolCatalog : ISymbolCatalog
{
    private readonly AppDbContext _db;

    public SymbolCatalog(AppDbContext db) => _db = db;

    public async Task<string?> GetNameAsync(string ticker, CancellationToken ct = default)
    {
        ticker = ticker.Trim().ToUpperInvariant();
        return await _db.Symbols
            .Where(s => s.Ticker == ticker)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetNamesAsync(
        IEnumerable<string> tickers, CancellationToken ct = default)
    {
        var set = tickers.Select(t => t.Trim().ToUpperInvariant()).Distinct().ToList();
        return await _db.Symbols
            .Where(s => set.Contains(s.Ticker))
            .ToDictionaryAsync(s => s.Ticker, s => s.Name, ct);
    }
}
