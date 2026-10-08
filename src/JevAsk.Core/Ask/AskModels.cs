using JevAsk.Core.Parsing;
using JevAsk.Core.Probability;

namespace JevAsk.Core.Ask;

public static class ProductCopy
{
    public const string Disclaimer = "Educational tool. Not financial advice.";
}

public sealed record MarketSeries(
    string Ticker,
    string Freshness,
    string Origin,
    IReadOnlyList<DailyBar> Bars);

public interface IMarketData
{
    Task<MarketSeries> GetAsync(string ticker, CancellationToken cancellationToken);
}

public sealed record Step(int N, string Title, string Detail);

public sealed record ChartPoint(DateOnly Date, double Close);

public sealed record AskResponse(
    string Question,
    ParsedIntent Intent,
    string Parser,
    bool ParserFellBack,
    string? AttemptedParser,
    bool ManualEdit,
    string DataFreshness,
    string DataOrigin,
    double Spot,
    DateOnly SpotDate,
    double Probability,
    double BandLow,
    double BandHigh,
    double MonteCarlo,
    double? Empirical,
    int EmpiricalSamples,
    double? Vol20,
    double? Vol60,
    double? Vol252,
    string VolWindow,
    int TradingDays,
    double TargetPrice,
    string Reasoning,
    IReadOnlyList<Step> Steps,
    IReadOnlyList<ChartPoint> Chart,
    string Disclaimer)
{
    public long Id { get; init; }
}

public static class DemoCatalog
{
    public static readonly string[] Tickers =
    [
        "SPY", "QQQ", "IWM", "AAPL", "MSFT", "NVDA", "AMZN", "GOOGL", "META",
        "TSLA", "AMD", "NFLX", "AVGO", "JPM", "DIS"
    ];

    public static readonly string[] Questions =
    [
        "Will NVDA close above 250 by end of month?",
        "Chance SPY drops 5% this month?",
        "Does TSLA touch 420 before Dec 20?",
        "Will AAPL finish below 300 by end of month?",
        "QQQ rises 3% in 10 days",
        "Will AMZN close above 280 by year end?",
        "Does META touch 800 before Dec 31?",
        "GOOGL close above 370 by end of month",
        "Will AMD drop 8% this month?",
        "NFLX rises 10% in 21 days",
        "AVGO touch 400 before Dec 20",
        "Will JPM close below 310 by next Friday?",
        "DIS gains 5% in 14 days",
        "Chance IWM drops 4% this month?"
    ];
}

public static class ExampleQuestions
{
    public static readonly string[] All = DemoCatalog.Questions;
}
