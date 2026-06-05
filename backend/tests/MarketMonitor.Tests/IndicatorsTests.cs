using MarketMonitor.Application.Suggestions;

namespace MarketMonitor.Tests;

public class IndicatorsTests
{
    [Fact]
    public void Sma_AveragesLastPeriodValues()
    {
        var values = new[] { 1m, 2m, 3m, 4m, 5m };
        Assert.Equal(4m, Indicators.Sma(values, 3)); // (3+4+5)/3
    }

    [Fact]
    public void Sma_ReturnsNull_WhenInsufficientData()
    {
        Assert.Null(Indicators.Sma(new[] { 1m, 2m }, 3));
    }

    [Fact]
    public void Ema_ReactsMoreToRecentValues_ThanSma()
    {
        // Rising series: EMA should sit above the SMA because it weights recents.
        var values = Enumerable.Range(1, 20).Select(i => (decimal)i).ToList();
        var ema = Indicators.Ema(values, 10)!.Value;
        var sma = Indicators.Sma(values, 10)!.Value;
        Assert.True(ema > sma, $"EMA {ema} should exceed SMA {sma} on a rising series.");
    }

    [Fact]
    public void Rsi_IsHigh_ForUninterruptedGains()
    {
        var closes = Enumerable.Range(1, 30).Select(i => (decimal)i).ToList();
        var rsi = Indicators.Rsi(closes, 14)!.Value;
        Assert.True(rsi > 90m, $"Expected overbought RSI, got {rsi}.");
    }

    [Fact]
    public void Rsi_IsLow_ForUninterruptedLosses()
    {
        var closes = Enumerable.Range(1, 30).Reverse().Select(i => (decimal)i).ToList();
        var rsi = Indicators.Rsi(closes, 14)!.Value;
        Assert.True(rsi < 10m, $"Expected oversold RSI, got {rsi}.");
    }

    [Fact]
    public void Rsi_ReturnsNull_WhenInsufficientData()
    {
        Assert.Null(Indicators.Rsi(new[] { 1m, 2m, 3m }, 14));
    }

    [Theory]
    [InlineData(new[] { 10.0, 20.0, 30.0 }, 100.0)] // latest at top of range
    [InlineData(new[] { 30.0, 20.0, 10.0 }, 0.0)]   // latest at bottom
    [InlineData(new[] { 10.0, 30.0, 20.0 }, 50.0)]  // latest mid-range
    public void PercentOfRange_PlacesLatestWithinWindow(double[] raw, double expected)
    {
        var closes = raw.Select(d => (decimal)d).ToList();
        var pct = Indicators.PercentOfRange(closes, closes.Count)!.Value;
        Assert.Equal((decimal)expected, pct, precision: 2);
    }
}
