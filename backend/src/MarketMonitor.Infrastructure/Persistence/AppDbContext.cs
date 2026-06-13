using MarketMonitor.Domain.Entities;
using MarketMonitor.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Infrastructure.Persistence;

/// <summary>
/// EF Core context. Inherits Identity's schema and adds the app's own tables.
/// The same model is used for both SQL Server Express (local) and Azure SQL
/// (prod) — only the connection string/provider registration differs.
/// </summary>
public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Symbol> Symbols => Set<Symbol>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Holding> Holdings => Set<Holding>();
    public DbSet<Trade> Trades => Set<Trade>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Symbol>(e =>
        {
            e.HasIndex(s => s.Ticker).IsUnique();
            e.Property(s => s.Ticker).HasMaxLength(12).IsRequired();
            e.Property(s => s.Name).HasMaxLength(120).IsRequired();
            e.Property(s => s.Exchange).HasMaxLength(40);
            e.Property(s => s.Sector).HasMaxLength(60);
        });

        builder.Entity<WatchlistItem>(e =>
        {
            e.Property(w => w.Ticker).HasMaxLength(12).IsRequired();
            // A user can pin a given symbol only once.
            e.HasIndex(w => new { w.UserId, w.Ticker }).IsUnique();
        });

        builder.Entity<Portfolio>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(80).IsRequired();
            e.Property(p => p.CashBalance).HasPrecision(18, 2);
            e.Property(p => p.InitialCash).HasPrecision(18, 2);
            e.HasIndex(p => p.UserId);
            e.HasMany(p => p.Holdings).WithOne(h => h.Portfolio!).HasForeignKey(h => h.PortfolioId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Trades).WithOne(t => t.Portfolio!).HasForeignKey(t => t.PortfolioId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Holding>(e =>
        {
            e.Property(h => h.Ticker).HasMaxLength(12).IsRequired();
            e.Property(h => h.Quantity).HasPrecision(18, 4);
            e.Property(h => h.AverageCost).HasPrecision(18, 4);
            e.HasIndex(h => new { h.PortfolioId, h.Ticker }).IsUnique();
        });

        builder.Entity<Trade>(e =>
        {
            e.Property(t => t.Ticker).HasMaxLength(12).IsRequired();
            e.Property(t => t.Quantity).HasPrecision(18, 4);
            e.Property(t => t.Price).HasPrecision(18, 4);
        });

        ApplyObjectNamePrefix(builder, TablePrefix);
    }

    /// <summary>Project code prefixed onto every database object (incl. Identity tables).</summary>
    public const string TablePrefix = "mm_";

    /// <summary>
    /// Prefixes every table with the project code so this database can be shared by
    /// multiple applications without name collisions. EF's conventions derive the
    /// primary-key, foreign-key, and index names from the (prefixed) table name, so
    /// those become prefixed too (e.g. PK_mm_Symbols, IX_mm_Holdings_PortfolioId_Ticker).
    /// The migrations-history table is prefixed separately via the Npgsql options.
    /// </summary>
    private static void ApplyObjectNamePrefix(ModelBuilder builder, string prefix)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is not null && !table.StartsWith(prefix, StringComparison.Ordinal))
                entity.SetTableName(prefix + table);
        }
    }
}
