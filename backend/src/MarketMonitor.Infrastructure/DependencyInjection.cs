using MarketMonitor.Application.Interfaces;
using MarketMonitor.Application.Services;
using MarketMonitor.Application.Suggestions;
using MarketMonitor.Infrastructure.Auth;
using MarketMonitor.Infrastructure.Identity;
using MarketMonitor.Infrastructure.MarketData;
using MarketMonitor.Infrastructure.Persistence;
using MarketMonitor.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketMonitor.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence, identity, market-data, and the suggestion/business
    /// services. JWT *authentication* (bearer validation) is configured in the API
    /// project; this only registers the token-issuing service and options.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddIdentityCore(services);
        AddOptionsBinding(services, configuration);
        AddMarketData(services, configuration);
        AddBusinessServices(services);

        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        // Open-source PostgreSQL via the Npgsql provider. The same EF model is used
        // for local dev and production; only the connection string differs. It comes
        // from appsettings.Development.json locally and from the
        // ConnectionStrings__DefaultConnection env var in production (e.g. Render).
        var provider = configuration["Database:Provider"] ?? "Postgres";
        var raw = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No 'DefaultConnection' connection string configured. " +
                "Set it in appsettings.Development.json (local) or the " +
                "ConnectionStrings__DefaultConnection environment variable (prod).");

        var connectionString = NormalizePostgresConnectionString(raw);

        services.AddDbContext<AppDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "postgres":
                case "postgresql":
                    options.UseNpgsql(connectionString, o =>
                    {
                        o.EnableRetryOnFailure(); // resilient to transient drops / cold starts
                        // Own migrations-history table so the DB can be shared across apps.
                        o.MigrationsHistoryTable($"__{AppDbContext.TablePrefix}EFMigrationsHistory");
                    });
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported Database:Provider '{provider}'. Supported: 'Postgres'.");
            }
        });
    }

    /// <summary>
    /// Some managed Postgres providers hand out a
    /// "postgres://user:pass@host:port/db" URL, but Npgsql expects keyword=value.
    /// Convert URLs; pass keyword strings through unchanged.
    /// </summary>
    private static string NormalizePostgresConnectionString(string cs)
    {
        if (!cs.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !cs.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return cs;

        var uri = new Uri(cs);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = Npgsql.SslMode.Require,      // encrypt; managed Postgres requires TLS
        };
        return builder.ConnectionString;
    }

    private static void AddIdentityCore(IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>();
    }

    private static void AddOptionsBinding(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MarketDataOptions>(configuration.GetSection(MarketDataOptions.SectionName));

        // Fail fast on a missing/short signing key.
        services.AddOptions<JwtOptions>().Validate(
            o => !string.IsNullOrWhiteSpace(o.SigningKey) && o.SigningKey.Length >= 32,
            "Jwt:SigningKey must be configured and at least 32 characters (use user-secrets locally).");
    }

    private static void AddMarketData(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<MockMarketDataProvider>();

        var options = configuration.GetSection(MarketDataOptions.SectionName).Get<MarketDataOptions>()
                      ?? new MarketDataOptions();
        var provider = (options.Provider ?? "Finnhub").Trim();
        bool hasKey = !string.IsNullOrWhiteSpace(options.ApiKey);

        // Typed HttpClients for every live provider (cheap if unused).
        services.AddHttpClient<FinnhubMarketDataProvider>(c => c.Timeout = TimeSpan.FromSeconds(10));
        services.AddHttpClient<TwelveDataMarketDataProvider>(c => c.Timeout = TimeSpan.FromSeconds(10));

        // Resolve the concrete live provider chosen by configuration.
        Func<IServiceProvider, IMarketDataProvider>? liveResolver = provider.ToLowerInvariant() switch
        {
            "finnhub" => sp => sp.GetRequiredService<FinnhubMarketDataProvider>(),
            "twelvedata" => sp => sp.GetRequiredService<TwelveDataMarketDataProvider>(),
            "mock" => null,
            _ => sp => sp.GetRequiredService<FinnhubMarketDataProvider>()
        };

        if (liveResolver is not null && hasKey)
        {
            // Wrap the live provider with caching + rate-limit backoff + mock fallback.
            services.AddSingleton<IMarketDataProvider>(sp => new ResilientMarketDataProvider(
                liveResolver(sp),
                sp.GetRequiredService<MockMarketDataProvider>(),
                sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                sp.GetRequiredService<IOptions<MarketDataOptions>>(),
                sp.GetRequiredService<ILogger<ResilientMarketDataProvider>>()));
        }
        else
        {
            // Explicit "Mock" or no API key: run fully on deterministic mock data.
            services.AddSingleton<IMarketDataProvider>(sp => sp.GetRequiredService<MockMarketDataProvider>());
        }
    }

    private static void AddBusinessServices(IServiceCollection services)
    {
        services.AddSingleton(SuggestionEngine.CreateDefault());
        services.AddScoped<ISuggestionService, SuggestionService>();
        services.AddScoped<ISymbolCatalog, SymbolCatalog>();
        services.AddScoped<ITopSharesService, TopSharesService>();
        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
    }
}
