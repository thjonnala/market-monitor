namespace MarketMonitor.Application.Suggestions;

/// <summary>
/// Pure, side-effect-free technical indicator math. Kept standalone so it is
/// trivially unit-testable and reusable across signal rules.
/// </summary>
public static class Indicators
{
    /// <summary>
    /// Simple moving average of the last <paramref name="period"/> values.
    /// Returns null when there is not enough data.
    /// </summary>
    public static decimal? Sma(IReadOnlyList<decimal> values, int period)
    {
        if (period <= 0 || values.Count < period) return null;

        decimal sum = 0m;
        for (int i = values.Count - period; i < values.Count; i++)
            sum += values[i];

        return sum / period;
    }

    /// <summary>
    /// Exponential moving average over the whole series using the standard
    /// 2/(period+1) smoothing factor, seeded with the first value.
    /// Returns null when there is not enough data.
    /// </summary>
    public static decimal? Ema(IReadOnlyList<decimal> values, int period)
    {
        if (period <= 0 || values.Count < period) return null;

        decimal k = 2m / (period + 1);
        decimal ema = values[0];
        for (int i = 1; i < values.Count; i++)
            ema = (values[i] - ema) * k + ema;

        return ema;
    }

    /// <summary>
    /// Wilder's Relative Strength Index over <paramref name="period"/> closes.
    /// Returns a value in [0, 100], or null when there is not enough data.
    /// </summary>
    public static decimal? Rsi(IReadOnlyList<decimal> closes, int period = 14)
    {
        if (period <= 0 || closes.Count <= period) return null;

        decimal gain = 0m, loss = 0m;

        // Initial average over the first `period` changes.
        for (int i = 1; i <= period; i++)
        {
            decimal change = closes[i] - closes[i - 1];
            if (change >= 0) gain += change;
            else loss -= change;
        }

        decimal avgGain = gain / period;
        decimal avgLoss = loss / period;

        // Smooth across the remaining changes (Wilder smoothing).
        for (int i = period + 1; i < closes.Count; i++)
        {
            decimal change = closes[i] - closes[i - 1];
            decimal up = change > 0 ? change : 0m;
            decimal down = change < 0 ? -change : 0m;

            avgGain = (avgGain * (period - 1) + up) / period;
            avgLoss = (avgLoss * (period - 1) + down) / period;
        }

        if (avgLoss == 0m) return 100m;
        if (avgGain == 0m) return 0m;

        decimal rs = avgGain / avgLoss;
        return 100m - 100m / (1 + rs);
    }

    /// <summary>
    /// Where the latest close sits within the [min, max] of the recent window,
    /// expressed as a percentage in [0, 100]. Null when there is not enough data.
    /// </summary>
    public static decimal? PercentOfRange(IReadOnlyList<decimal> closes, int window)
    {
        if (window <= 1 || closes.Count < window) return null;

        decimal min = decimal.MaxValue, max = decimal.MinValue;
        for (int i = closes.Count - window; i < closes.Count; i++)
        {
            if (closes[i] < min) min = closes[i];
            if (closes[i] > max) max = closes[i];
        }

        if (max == min) return 50m;

        decimal latest = closes[^1];
        return (latest - min) / (max - min) * 100m;
    }
}
