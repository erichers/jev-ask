using System.Globalization;
using JevAsk.Core.Parsing;
using JevAsk.Core.Probability;

namespace JevAsk.Core.Ask;

public sealed class AskException : Exception
{
    public AskException(string message) : base(message)
    {
    }
}

public sealed class AskService
{
    private readonly IQuestionParser _parser;
    private readonly IMarketData _market;
    private readonly ProbabilityEngine _engine;
    private readonly int _paths;
    private readonly int _seed;

    public AskService(IQuestionParser parser, IMarketData market, ProbabilityEngine engine, int monteCarloPaths, int seed)
    {
        _parser = parser;
        _market = market;
        _engine = engine;
        _paths = monteCarloPaths;
        _seed = seed;
    }

    public async Task<AskResponse> AskAsync(string? question, DateOnly asOf, CancellationToken cancellationToken)
    {
        var text = question?.Trim() ?? "";
        if (text.Length == 0)
            throw new AskException("Type a market question first.");

        var parsed = await _parser.ParseAsync(text, asOf, cancellationToken);
        if (parsed.Intent is null)
            throw new AskException(parsed.Error ?? "Could not parse that question.");

        return await BuildAsync(parsed.Intent, parsed.ParserName, parsed.FellBack, parsed.AttemptedParser, false, asOf, cancellationToken);
    }

    public async Task<AskResponse> RecomputeAsync(
        string? ticker,
        string? condition,
        string? style,
        string? levelMode,
        double? level,
        DateOnly? expiry,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new AskException("Enter a ticker.");
        if (level is null || level <= 0)
            throw new AskException("Enter a positive level.");
        if (expiry is null)
            throw new AskException("Enter a date.");

        var direction = (condition ?? "").Trim().ToLowerInvariant() switch
        {
            "above" => BarrierDirection.Above,
            "below" => BarrierDirection.Below,
            _ => throw new AskException("Condition must be above or below.")
        };
        var barrierStyle = (style ?? "").Trim().ToLowerInvariant() switch
        {
            "close" => BarrierStyle.Close,
            "touch" => BarrierStyle.Touch,
            _ => throw new AskException("Condition style must be close or touch.")
        };
        var mode = (levelMode ?? "").Trim().ToLowerInvariant() switch
        {
            "absolute" => LevelMode.Absolute,
            "percent" => LevelMode.Percent,
            _ => throw new AskException("Level must be a price or a percent.")
        };

        var symbol = ticker.Trim().ToUpperInvariant();
        var summary = Describe(symbol, direction, barrierStyle, mode, level.Value, expiry.Value);
        var intent = new ParsedIntent(symbol, direction, barrierStyle, mode, level.Value, expiry.Value, false, summary);
        return await BuildAsync(intent, "manual-edit", false, null, true, asOf, cancellationToken);
    }

    private async Task<AskResponse> BuildAsync(
        ParsedIntent intent,
        string parser,
        bool fellBack,
        string? attempted,
        bool manual,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {
        var series = await _market.GetAsync(intent.Ticker, cancellationToken);
        var estimate = _engine.Estimate(new EstimateInput(
            series.Bars,
            intent.Level,
            intent.LevelMode,
            intent.Direction,
            intent.Style,
            asOf,
            intent.Expiry,
            _paths,
            _seed));

        if (!estimate.Ok)
            throw new AskException(estimate.Error ?? "Could not estimate a probability.");

        var resolved = intent with { Direction = estimate.Direction };
        var above = estimate.Direction == BarrierDirection.Above;
        var touch = resolved.Style == BarrierStyle.Touch;
        var reasoning = Reasoning(resolved, series, estimate, above, touch, _paths);
        var steps = Steps(resolved, series, estimate, above, touch, _paths);

        return new AskResponse(
            resolved.RawText,
            resolved,
            parser,
            fellBack,
            attempted,
            manual,
            series.Freshness,
            series.Origin,
            estimate.Spot,
            estimate.SpotDate,
            estimate.Probability,
            estimate.BandLow,
            estimate.BandHigh,
            estimate.MonteCarlo,
            estimate.Empirical,
            estimate.EmpiricalSamples,
            estimate.Vol20,
            estimate.Vol60,
            estimate.Vol252,
            estimate.VolWindow,
            estimate.TradingDays,
            estimate.TargetPrice,
            reasoning,
            steps,
            estimate.Chart.Select(bar => new ChartPoint(bar.Date, bar.Close)).ToList(),
            ProductCopy.Disclaimer);
    }

    private static string Reasoning(ParsedIntent intent, MarketSeries series, EstimateResult estimate, bool above, bool touch, int paths)
    {
        var verb = touch ? "touches" : "closes";
        var side = above ? "above" : "below";
        var levelText = intent.LevelMode == LevelMode.Percent
            ? $"{Px(estimate.TargetPrice)} ({intent.Level.ToString("0.##", CultureInfo.InvariantCulture)}% {side} the last close)"
            : Px(estimate.TargetPrice);
        var empirical = estimate.Empirical is null
            ? "There were not enough past windows for an empirical frequency."
            : $"In the stored history, {Pct(estimate.Empirical.Value)} of {estimate.EmpiricalSamples} windows of this length met the same test.";

        return $"{intent.Ticker} last closed at {Px(estimate.Spot)} on {estimate.SpotDate:yyyy-MM-dd}. " +
               $"The question asks whether it {verb} {side} {levelText} by {intent.Expiry:yyyy-MM-dd} " +
               $"({estimate.TradingDays} trading days, T = {estimate.Years.ToString("0.000", CultureInfo.InvariantCulture)} years). " +
               $"The point estimate uses {estimate.VolWindow} realized volatility of {Vol(estimate.PrimaryVol)}. " +
               $"A zero-drift lognormal model gives {Pct(estimate.Probability)}. " +
               $"The range is {Pct(estimate.BandLow)} to {Pct(estimate.BandHigh)}. " +
               $"Monte Carlo with {paths.ToString("N0", CultureInfo.InvariantCulture)} paths came out at {Pct(estimate.MonteCarlo)}. {empirical} " +
               $"Prices came from {series.Origin} and are marked {series.Freshness}.";
    }

    private static IReadOnlyList<Step> Steps(ParsedIntent intent, MarketSeries series, EstimateResult estimate, bool above, bool touch, int paths)
    {
        var volBits = new List<string>();
        if (estimate.Vol20 is not null)
            volBits.Add($"20-day {Vol(estimate.Vol20.Value)}");
        if (estimate.Vol60 is not null)
            volBits.Add($"60-day {Vol(estimate.Vol60.Value)}");
        if (estimate.Vol252 is not null)
            volBits.Add($"252-day {Vol(estimate.Vol252.Value)}");

        var bandText = estimate.BandMethod == "bootstrap"
            ? $"A bootstrap of the {estimate.VolWindow} returns gives {Pct(estimate.BandLow)} to {Pct(estimate.BandHigh)}."
            : $"Those vol windows give {Pct(estimate.BandLow)} to {Pct(estimate.BandHigh)}.";

        var empirical = estimate.Empirical is null
            ? $"Only {estimate.EmpiricalSamples} windows were available, so no empirical frequency is shown."
            : $"{Pct(estimate.Empirical.Value)} of {estimate.EmpiricalSamples} past windows of {estimate.TradingDays} trading days met this test.";

        return
        [
            new Step(1, "Data",
                $"Loaded {series.Bars.Count} daily closes for {intent.Ticker} from {series.Origin}. Spot {Px(estimate.Spot)} on {estimate.SpotDate:yyyy-MM-dd}. This result is {series.Freshness} data."),
            new Step(2, "Volatility",
                $"Realized volatility from log returns: {string.Join(", ", volBits)}. The point estimate uses the {estimate.VolWindow} vol, {Vol(estimate.PrimaryVol)}. {bandText}"),
            new Step(3, "Formula",
                $"{ProbabilityMath.Formula(touch, above)} S = {Px(estimate.Spot)}, K = {Px(estimate.TargetPrice)}, sigma = {estimate.PrimaryVol.ToString("0.000", CultureInfo.InvariantCulture)}, T = {estimate.Years.ToString("0.000", CultureInfo.InvariantCulture)}, trading days = {estimate.TradingDays}. Result {Pct(estimate.Probability)}."),
            new Step(4, "Monte Carlo",
                $"{paths.ToString("N0", CultureInfo.InvariantCulture)} geometric Brownian paths with the same vol and zero price drift. Share that met the condition: {Pct(estimate.MonteCarlo)}."),
            new Step(5, "History", empirical)
        ];
    }

    private static string Describe(string ticker, BarrierDirection direction, BarrierStyle style, LevelMode mode, double level, DateOnly expiry)
    {
        var side = direction == BarrierDirection.Above ? "above" : "below";
        var how = style == BarrierStyle.Touch ? "touch" : "close";
        var levelText = mode == LevelMode.Percent
            ? $"{level.ToString("0.##", CultureInfo.InvariantCulture)}%"
            : level.ToString("0.##", CultureInfo.InvariantCulture);
        return $"{ticker} {how} {side} {levelText} by {expiry:yyyy-MM-dd}";
    }

    private static string Pct(double probability) =>
        (probability * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";

    private static string Px(double price) =>
        price.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Vol(double sigma) =>
        (sigma * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
}
