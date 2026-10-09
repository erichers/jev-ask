using JevAsk.Core.Parsing;
using JevAsk.Core.Probability;

namespace JevAsk.Tests;

public class ProbabilityTests
{
    [Fact]
    public void Normal_cdf_matches_known_values()
    {
        Assert.Equal(0.5, ProbabilityMath.NormalCdf(0), 6);
        Assert.Equal(0.975, ProbabilityMath.NormalCdf(1.959963984540054), 3);
    }

    [Fact]
    public void Close_and_touch_match_independent_values()
    {
        var years = 21.0 / 252.0;
        Assert.Equal(0.04650899331499131, ProbabilityMath.Terminal(100, 110, 0.20, years, true), 6);
        Assert.InRange(ProbabilityMath.Touch(100, 110, 0.20, years, true), 0.07161735038415558 - 2e-6, 0.07161735038415558 + 2e-6);
        Assert.Equal(0.036244861143015616, ProbabilityMath.Terminal(100, 90, 0.20, years, false), 6);
        Assert.InRange(ProbabilityMath.Touch(100, 90, 0.20, years, false), 0.05387053565339264 - 2e-6, 0.05387053565339264 + 2e-6);
    }

    [Fact]
    public void Touch_is_at_least_the_terminal_probability()
    {
        var years = 63.0 / 252.0;
        var close = ProbabilityMath.Terminal(100, 120, 0.30, years, true);
        var touch = ProbabilityMath.Touch(100, 120, 0.30, years, true);
        Assert.True(touch + 1e-12 >= close);
        var closeDown = ProbabilityMath.Terminal(100, 80, 0.30, years, false);
        var touchDown = ProbabilityMath.Touch(100, 80, 0.30, years, false);
        Assert.True(touchDown + 1e-12 >= closeDown);
    }

    [Fact]
    public void Higher_volatility_raises_the_chance_of_a_distant_close()
    {
        var years = 63.0 / 252.0;
        var calm = ProbabilityMath.Terminal(100, 130, 0.15, years, true);
        var wild = ProbabilityMath.Terminal(100, 130, 0.45, years, true);
        Assert.True(wild > calm);
    }

    [Fact]
    public void Zero_volatility_is_deterministic()
    {
        Assert.Equal(0, ProbabilityMath.Terminal(100, 110, 0, 21.0 / 252.0, true));
        Assert.Equal(1, ProbabilityMath.Terminal(100, 90, 0, 21.0 / 252.0, true));
        Assert.Equal(1, ProbabilityMath.Touch(150, 150, 0.2, 0.1, true));
        Assert.Equal(1, ProbabilityMath.Touch(90, 100, 0.2, 0.1, false));
    }

    [Fact]
    public void Monte_Carlo_stays_near_the_analytic_close_and_touch()
    {
        var years = 21.0 / 252.0;
        var close = ProbabilityMath.Terminal(100, 110, 0.20, years, true);
        var touch = ProbabilityMath.Touch(100, 110, 0.20, years, true);
        var mcClose = ProbabilityMath.MonteCarlo(100, 110, 0.20, 21, true, false, 20000, 11);
        var mcTouch = ProbabilityMath.MonteCarlo(100, 110, 0.20, 21, true, true, 20000, 11);
        Assert.InRange(mcClose, close - 0.02, close + 0.02);
        Assert.InRange(mcTouch, touch - 0.02, touch + 0.02);

        var closeDown = ProbabilityMath.Terminal(100, 90, 0.20, years, false);
        var mcDown = ProbabilityMath.MonteCarlo(100, 90, 0.20, 21, false, false, 20000, 19);
        Assert.InRange(mcDown, closeDown - 0.02, closeDown + 0.02);
    }

    [Fact]
    public void Trading_day_count_skips_weekends()
    {
        Assert.Equal(2, ProbabilityMath.CountTradingDays(new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 9)));
        Assert.Equal(17, ProbabilityMath.CountTradingDays(new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 31)));
        Assert.Equal(0, ProbabilityMath.CountTradingDays(new DateOnly(2026, 10, 9), new DateOnly(2026, 10, 9)));
    }

    [Fact]
    public void Realized_vol_is_zero_for_a_flat_series_and_positive_for_a_move()
    {
        var flat = Enumerable.Repeat(100.0, 40).ToList();
        Assert.Equal(0, ProbabilityMath.AnnualizedVol(flat, 20));

        var stepped = new List<double> { 100 };
        for (var i = 0; i < 20; i++)
            stepped.Add(stepped[^1] * (i % 2 == 0 ? 1.01 : 0.99));
        var vol = ProbabilityMath.AnnualizedVol(stepped, 20);
        Assert.NotNull(vol);
        Assert.True(vol > 0.05);
    }

    [Fact]
    public void Engine_band_contains_the_point_estimate_and_matches_the_formula()
    {
        var bars = GbmBars(400, 100, 0.25, 4);
        var engine = new ProbabilityEngine();
        var result = engine.Estimate(new EstimateInput(
            bars,
            110,
            LevelMode.Absolute,
            BarrierDirection.Above,
            BarrierStyle.Close,
            bars[^1].Date,
            bars[^1].Date.AddDays(30),
            2000,
            5));

        Assert.True(result.Ok, result.Error);
        Assert.InRange(result.Probability, result.BandLow, result.BandHigh);
        Assert.InRange(result.MonteCarlo, 0, 1);
        Assert.NotNull(result.Empirical);
        Assert.InRange(result.Empirical!.Value, 0, 1);
        var expected = ProbabilityMath.Terminal(
            result.Spot,
            result.TargetPrice,
            result.PrimaryVol,
            result.Years,
            true);
        Assert.Equal(expected, result.Probability, 8);
        Assert.True(result.TradingDays > 0);
        Assert.Equal(BarrierDirection.Above, result.Direction);
    }

    [Fact]
    public void Engine_resolves_an_unspecified_touch_from_the_spot()
    {
        var bars = GbmBars(80, 250, 0.3, 8);
        var engine = new ProbabilityEngine();
        var above = engine.Estimate(new EstimateInput(
            bars, 300, LevelMode.Absolute, null, BarrierStyle.Touch,
            bars[^1].Date, bars[^1].Date.AddDays(40), 500, 2));
        Assert.Equal(BarrierDirection.Above, above.Direction);
        Assert.True(above.TargetPrice > above.Spot);

        var below = engine.Estimate(new EstimateInput(
            bars, 200, LevelMode.Absolute, null, BarrierStyle.Touch,
            bars[^1].Date, bars[^1].Date.AddDays(40), 500, 2));
        Assert.Equal(BarrierDirection.Below, below.Direction);
    }

    [Fact]
    public void Percent_drop_sets_a_lower_target()
    {
        var bars = GbmBars(80, 500, 0.16, 9);
        var engine = new ProbabilityEngine();
        var result = engine.Estimate(new EstimateInput(
            bars, 5, LevelMode.Percent, BarrierDirection.Below, BarrierStyle.Close,
            bars[^1].Date, bars[^1].Date.AddDays(20), 500, 3));
        Assert.True(result.Ok, result.Error);
        Assert.Equal(result.Spot * 0.95, result.TargetPrice, 6);
    }

    [Fact]
    public void Past_expiry_is_rejected()
    {
        var bars = GbmBars(40, 100, 0.2, 1);
        var engine = new ProbabilityEngine();
        var result = engine.Estimate(new EstimateInput(
            bars, 100, LevelMode.Absolute, BarrierDirection.Above, BarrierStyle.Close,
            bars[^1].Date, bars[^1].Date.AddDays(-3), 200, 1));
        Assert.False(result.Ok);
    }

    private static List<DailyBar> GbmBars(int count, double start, double sigma, int seed)
    {
        var rng = new Random(seed);
        var price = start;
        var day = new DateOnly(2024, 1, 2);
        var bars = new List<DailyBar>();
        var dt = 1.0 / 252.0;
        var nu = -0.5 * sigma * sigma;
        while (bars.Count < count)
        {
            if (day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                bars.Add(new DailyBar(day, price));
                var u1 = 1.0 - rng.NextDouble();
                var u2 = 1.0 - rng.NextDouble();
                var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                price *= Math.Exp(nu * dt + sigma * Math.Sqrt(dt) * z);
            }

            day = day.AddDays(1);
        }

        return bars;
    }
}
