namespace JevAsk.Core.Probability;

public static class ProbabilityMath
{
    public static double NormalCdf(double x) => 0.5 * (1.0 + Erf(x / Math.Sqrt(2.0)));

    // Abramowitz and Stegun 7.1.26. Absolute error is about 1.5e-7.
    private static double Erf(double x)
    {
        var sign = x < 0 ? -1.0 : 1.0;
        x = Math.Abs(x);
        var t = 1.0 / (1.0 + 0.3275911 * x);
        var y = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x);
        return sign * y;
    }

    public static double Terminal(double spot, double barrier, double sigma, double years, bool above)
    {
        if (spot <= 0 || barrier <= 0)
            return 0;
        if (years <= 1e-12 || sigma <= 1e-8)
            return above ? (spot > barrier ? 1 : 0) : (spot < barrier ? 1 : 0);

        var nu = -0.5 * sigma * sigma;
        var z = (Math.Log(spot / barrier) + nu * years) / (sigma * Math.Sqrt(years));
        var pAbove = NormalCdf(z);
        return Clamp(above ? pAbove : 1.0 - pAbove);
    }

    /// <summary>
    /// First-passage probability for zero price drift (log drift v = -sigma^2/2).
    /// </summary>
    public static double Touch(double spot, double barrier, double sigma, double years, bool above)
    {
        if (spot <= 0 || barrier <= 0)
            return 0;
        if (above && spot >= barrier)
            return 1;
        if (!above && spot <= barrier)
            return 1;
        if (years <= 1e-12 || sigma <= 1e-8)
            return 0;

        // Daily closes miss intra-day hits. The Broadie-Glasserman-Kou shift
        // makes the continuous barrier formula line up with daily monitoring.
        var shift = Math.Exp(0.5825971579390107 * sigma * Math.Sqrt(1.0 / 252.0));
        var monitored = above ? barrier * shift : barrier / shift;
        var nu = -0.5 * sigma * sigma;
        var s = sigma * Math.Sqrt(years);
        if (above)
        {
            var b = Math.Log(monitored / spot);
            var p = NormalCdf((nu * years - b) / s) + (spot / monitored) * NormalCdf((-nu * years - b) / s);
            return Clamp(p);
        }

        var down = Math.Log(spot / monitored);
        var lower = NormalCdf((-nu * years - down) / s) + (spot / monitored) * NormalCdf((nu * years - down) / s);
        return Clamp(lower);
    }

    public static double? AnnualizedVol(IReadOnlyList<double> closes, int window)
    {
        if (window < 2 || closes.Count < window + 1)
            return null;

        var start = closes.Count - window - 1;
        var returns = new double[window];
        for (var i = 0; i < window; i++)
        {
            var prev = closes[start + i];
            var next = closes[start + i + 1];
            if (prev <= 0 || next <= 0)
                return null;
            returns[i] = Math.Log(next / prev);
        }

        return Math.Sqrt(SampleVariance(returns)) * Math.Sqrt(252.0);
    }

    public static double MonteCarlo(
        double spot,
        double barrier,
        double sigma,
        int tradingDays,
        bool above,
        bool touch,
        int paths,
        int seed)
    {
        if (tradingDays <= 0 || sigma <= 1e-8)
        {
            if (touch)
                return above ? (spot >= barrier ? 1 : 0) : (spot <= barrier ? 1 : 0);
            return above ? (spot > barrier ? 1 : 0) : (spot < barrier ? 1 : 0);
        }

        var rng = new Random(seed);
        var nu = -0.5 * sigma * sigma;
        var dt = 1.0 / 252.0;
        var step = sigma * Math.Sqrt(dt);
        var hits = 0;
        for (var path = 0; path < paths; path++)
        {
            var price = spot;
            var touched = false;
            for (var day = 0; day < tradingDays; day++)
            {
                var z = NextGaussian(rng);
                price *= Math.Exp(nu * dt + step * z);
                if (!touched && (above ? price >= barrier : price <= barrier))
                    touched = true;
            }

            var met = touch
                ? touched
                : above ? price > barrier : price < barrier;
            if (met)
                hits++;
        }

        return hits / (double)paths;
    }

    public static int CountTradingDays(DateOnly startExclusive, DateOnly endInclusive)
    {
        if (endInclusive <= startExclusive)
            return 0;

        var count = 0;
        for (var day = startExclusive.AddDays(1); day <= endInclusive; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                count++;
        }

        return count;
    }

    public static string Formula(bool touch, bool above)
    {
        if (!touch && above)
            return "Phi( (ln(S/K) + v*T) / (sigma*sqrt(T)) ) with v = -sigma^2/2";
        if (!touch)
            return "1 - Phi( (ln(S/K) + v*T) / (sigma*sqrt(T)) ) with v = -sigma^2/2";
        if (above)
            return "Daily touch, barrier shifted by the Broadie-Glasserman-Kou correction, then Phi( (v*T - b) / (sigma*sqrt(T)) ) + (S/K) * Phi( (-v*T - b) / (sigma*sqrt(T)) ), b = ln(K/S), v = -sigma^2/2";
        return "Daily touch, barrier shifted by the Broadie-Glasserman-Kou correction, then Phi( (-v*T - b) / (sigma*sqrt(T)) ) + (S/L) * Phi( (v*T - b) / (sigma*sqrt(T)) ), b = ln(S/L), v = -sigma^2/2";
    }

    private static double NextGaussian(Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    private static double SampleVariance(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
            return 0;
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

    private static double Clamp(double p)
    {
        if (double.IsNaN(p) || double.IsInfinity(p))
            return 0;
        if (p < 0)
            return 0;
        if (p > 1)
            return 1;
        return p;
    }
}
