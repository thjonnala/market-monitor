using MarketMonitor.Domain.Entities;
using MarketMonitor.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarketMonitor.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and seeds a handful of sample symbols plus a demo user so
/// the app is usable immediately after setup. Safe to run repeatedly.
/// </summary>
public static class DbSeeder
{
    public const string DemoEmail = "demo@marketmonitor.local";
    public const string DemoPassword = "Demo1234!";
    public const string DemoDisplayName = "Demo User";

    private static readonly (string Ticker, string Name, string Sector, bool Curated)[] Seed =
    {
        ("AAPL", "Apple Inc.", "Technology", true),
        ("MSFT", "Microsoft Corporation", "Technology", true),
        ("GOOGL", "Alphabet Inc.", "Communication Services", true),
        ("AMZN", "Amazon.com, Inc.", "Consumer Discretionary", true),
        ("NVDA", "NVIDIA Corporation", "Technology", true),
        ("META", "Meta Platforms, Inc.", "Communication Services", true),
        ("TSLA", "Tesla, Inc.", "Consumer Discretionary", true),
        ("JPM", "JPMorgan Chase & Co.", "Financials", true),
        ("AMD", "Advanced Micro Devices, Inc.", "Technology", true),
        ("NFLX", "Netflix, Inc.", "Communication Services", true),
        ("BAC", "Bank of America Corporation", "Financials", true),
        ("KO", "The Coca-Cola Company", "Consumer Staples", true),
        ("V", "Visa Inc.", "Financials", false),
        ("DIS", "The Walt Disney Company", "Communication Services", false),
    };

    public static async Task SeedAsync(IServiceProvider services, bool applyMigrations = true)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var db = sp.GetRequiredService<AppDbContext>();

        if (applyMigrations)
        {
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync();
        }

        await SeedSymbolsAsync(db, logger);
        await SeedDemoUserAsync(sp, logger);
    }

    private static async Task SeedSymbolsAsync(AppDbContext db, ILogger logger)
    {
        var existing = await db.Symbols.Select(s => s.Ticker).ToListAsync();
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = Seed
            .Where(s => !existingSet.Contains(s.Ticker))
            .Select(s => new Symbol
            {
                Ticker = s.Ticker,
                Name = s.Name,
                Sector = s.Sector,
                Exchange = "NASDAQ/NYSE",
                IsCurated = s.Curated
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            db.Symbols.AddRange(toAdd);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} symbols.", toAdd.Count);
        }
    }

    private static async Task SeedDemoUserAsync(IServiceProvider sp, ILogger logger)
    {
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(DemoEmail) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true,
            DisplayName = DemoDisplayName
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (result.Succeeded)
            logger.LogInformation("Seeded demo user {Email}.", DemoEmail);
        else
            logger.LogWarning("Failed to seed demo user: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
