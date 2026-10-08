using JevAsk.Core.Parsing;

namespace JevAsk.Core.Probability;

public sealed record DailyBar(DateOnly Date, double Close);

public sealed record EstimateInput(
    IReadOnlyList<DailyBar> Bars,
    double Level,
    LevelMode LevelMode,
    BarrierDirection? Direction,
    BarrierStyle Style,
    DateOnly AsOf,
    DateOnly Expiry,
    int MonteCarloPaths,
    int Seed);

public sealed record EstimateResult(
    bool Ok,
    string? Error,
    double Spot,
    DateOnly SpotDate,
    double Probability,
    double BandLow,
    double BandHigh,
    string BandMethod,
    double MonteCarlo,
    double? Empirical,
    int EmpiricalSamples,
    double? Vol20,
    double? Vol60,
    double? Vol252,
    double PrimaryVol,
    string VolWindow,
    int TradingDays,
    double Years,
    double TargetPrice,
    BarrierDirection Direction,
    IReadOnlyList<DailyBar> Chart);

public sealed class ProbabilityEngine
{
    public EstimateResult Estimate(EstimateInput input)
    {
        var empty = Empty();
        if (input.Bars.Count == 0)
            return empty with { Error = "No prices were loaded." };

        var bars = input.Bars.OrderBy(bar => bar.Date).ToList();
        var spotBar = bars.LastOrDefault(bar => bar.Date <= input.AsOf);
        if (spotBar is null)
            return empty with { Error = "No close on or before that date." };

        var spotIndex = bars.FindIndex(bar => bar.Date == spotBar.Date && bar.Close == spotBar.Close);
        var history = bars.Take(spotIndex + 1).ToList();
        var spot = spotBar.Close;
        if (spot <= 0)
            return empty with { Error = "The last close is not a usable price." };

        if (input.Expiry < spotBar.Date)
            return empty with { Error = "That date is before the last close." };

        var direction = input.Direction;
        if (direction is null)
        {
            direction = input.LevelMode == LevelMode.Percent
                ? BarrierDirection.Above
                : input.Level >= spot ? BarrierDirection.Above : BarrierDirection.Below;
        }

        var above = direction == BarrierDirection.Above;
        double target;
        if (input.LevelMode == LevelMode.Percent)
        {
            var factor = input.Level / 100.0;
            target = above ? spot * (1.0 + factor) : spot * (1.0 - factor);
        }
        else
        {
            target = input.Level;
        }

        if (target <= 0)
            return empty with { Error = "That percent move leaves no positive price." };

        var tradingDays = ProbabilityMath.CountTradingDays(spotBar.Date, input.Expiry);
        var years = tradingDays / 252.0;
        var closes = history.Select(bar => bar.Close).ToList();
        var vol20 = ProbabilityMath.AnnualizedVol(closes, 20);
        var vol60 = ProbabilityMath.AnnualizedVol(closes, 60);
        var vol252 = ProbabilityMath.AnnualizedVol(closes, 252);

        double primary;
        string window;
        if (vol60 is not null)
        {
            primary = vol60.Value;
            window = "60-day";
        }
        else if (vol20 is not null)
        {
            primary = vol20.Value;
            window = "20-day";
        }
        else if (vol252 is not null)
        {
            primary = vol252.Value;
            window = "252-day";
        }
        else
        {
            return empty with { Error = "Not enough return history to estimate volatility." };
        }

        var touch = input.Style == BarrierStyle.Touch;
        var probability = Chance(spot, target, primary, years, above, touch);

        var windowProbs = new List<double>();
        if (vol20 is not null)
            windowProbs.Add(Chance(spot, target, vol20.Value, years, above, touch));
        if (vol60 is not null)
            windowProbs.Add(Chance(spot, target, vol60.Value, years, above, touch));
        if (vol252 is not null)
            windowProbs.Add(Chance(spot, target, vol252.Value, years, above, touch));

        double bandLow;
        double bandHigh;
        string bandMethod;
        if (windowProbs.Count >= 2)
        {
            bandLow = windowProbs.Min();
            bandHigh = windowProbs.Max();
            bandMethod = "windows";
        }
        else
        {
            (bandLow, bandHigh) = Bootstrap(closes, WindowLength(window), spot, target, years, above, touch, input.Seed + 17);
            bandMethod = "bootstrap";
        }

        bandLow = Math.Min(bandLow, probability);
        bandHigh = Math.Max(bandHigh, probability);

        var paths = Math.Clamp(input.MonteCarloPaths, 200, 20000);
        var monteCarlo = ProbabilityMath.MonteCarlo(spot, target, primary, tradingDays, above, touch, paths, input.Seed);
        var (empirical, samples) = Empirical(history, tradingDays, input.Level, input.LevelMode, above, touch);

        var chartStart = Math.Max(0, history.Count - 252);
        var chart = history.Skip(chartStart).ToList();

        return new EstimateResult(
            true,
            null,
            spot,
            spotBar.Date,
            probability,
            bandLow,
            bandHigh,
            bandMethod,
            monteCarlo,
            empirical,
            samples,
            vol20,
            vol60,
            vol252,
            primary,
            window,
            tradingDays,
            years,
            target,
            direction.Value,
            chart);
    }

    private static (double Low, double High) Bootstrap(
        IReadOnlyList<double> closes,
        int window,
        double spot,
        double target,
        double years,
        bool above,
        bool touch,
        int seed)
    {
        if (closes.Count < window + 1 || window < 2)
            return (0, 1);

        var start = closes.Count - window - 1;
        var returns = new double[window];
        for (var i = 0; i < window; i++)
            returns[i] = Math.Log(closes[start + i + 1] / closes[start + i]);

        var rng = new Random(seed);
        var probs = new List<double>(200);
        var sample = new double[window];
        for (var draw = 0; draw < 200; draw++)
        {
            for (var i = 0; i < window; i++)
                sample[i] = returns[rng.Next(window)];
            var vol = Math.Sqrt(SampleVariance(sample)) * Math.Sqrt(252.0);
            probs.Add(Chance(spot, target, vol, years, above, touch));
        }

        probs.Sort();
        var lowIndex = (int)Math.Floor(0.10 * (probs.Count - 1));
        var highIndex = (int)Math.Ceiling(0.90 * (probs.Count - 1));
        return (probs[lowIndex], probs[highIndex]);
    }

    private static (double? Rate, int Samples) Empirical(
        IReadOnlyList<DailyBar> history,
        int horizon,
        double level,
        LevelMode levelMode,
        bool above,
        bool touch)
    {
        if (horizon <= 0 || history.Count < horizon + 2)
            return (null, 0);

        var closes = history.Select(bar => bar.Close).ToList();
        var met = 0;
        var total = 0;
        for (var i = 0; i + horizon < closes.Count; i++)
        {
            var start = closes[i];
            if (start <= 0)
                continue;

            double threshold = levelMode == LevelMode.Percent
                ? above ? start * (1.0 + level / 100.0) : start * (1.0 - level / 100.0)
                : level;

            if (threshold <= 0)
                continue;

            if (levelMode == LevelMode.Absolute)
            {
                if (above && start >= threshold)
                    continue;
                if (!above && start <= threshold)
                    continue;
            }

            var ok = false;
            if (!touch)
            {
                var end = closes[i + horizon];
                ok = above ? end > threshold : end < threshold;
            }
            else
            {
                for (var k = 1; k <= horizon; k++)
                {
                    var price = closes[i + k];
                    if (above ? price >= threshold : price <= threshold)
                    {
                        ok = true;
                        break;
                    }
                }
            }

            total++;
            if (ok)
                met++;
        }

        if (total < 30)
            return (null, total);
        return (met / (double)total, total);
    }

    private static double Chance(double spot, double barrier, double sigma, double years, bool above, bool touch) =>
        touch
            ? ProbabilityMath.Touch(spot, barrier, sigma, years, above)
            : ProbabilityMath.Terminal(spot, barrier, sigma, years, above);

    private static int WindowLength(string window) => window switch
    {
        "20-day" => 20,
        "252-day" => 252,
        _ => 60
    };

    private static double SampleVariance(IReadOnlyList<double> values)
    {
        double sum = 0;
        foreach (var value in values)
            sum += value;
        var mean = sum / values.Count;
        double ss = 0;
        foreach (var value in values)
        {
            var d = value - mean;
            ss += d * d;
        }

        return ss / (values.Count - 1);
    }

    private static EstimateResult Empty() => new(
        false,
        "Could not estimate a probability.",
        0,
        default,
        0,
        0,
        0,
        "windows",
        0,
        null,
        0,
        null,
        null,
        null,
        0,
        "60-day",
        0,
        0,
        0,
        BarrierDirection.Above,
        []);
}
