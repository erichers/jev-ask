using JevAsk.Core.Parsing;

namespace JevAsk.Tests;

public class ParserTests
{
    private static readonly DateOnly AsOf = new(2026, 10, 7);
    private readonly RuleBasedParser _parser = new();

    public static IEnumerable<object[]> Questions()
    {
        yield return Row("Will NVDA close above 150 by Friday?", "NVDA", "above", "close", "absolute", 150, "2026-10-09");
        yield return Row("Chance SPY drops 5% this month?", "SPY", "below", "close", "percent", 5, "2026-10-31");
        yield return Row("Does TSLA touch 300 before Dec 20?", "TSLA", null, "touch", "absolute", 300, "2026-12-20");
        yield return Row("Will Apple finish below 200 by end of month?", "AAPL", "below", "close", "absolute", 200, "2026-10-31");
        yield return Row("Nvidia above $180 next week", "NVDA", "above", "close", "absolute", 180, "2026-10-14");
        yield return Row("QQQ rises 3% in 10 days", "QQQ", "above", "close", "percent", 3, "2026-10-17");
        yield return Row("Will MSFT close under 400 by next Friday?", "MSFT", "below", "close", "absolute", 400, "2026-10-16");
        yield return Row("Tesla hits 250 before November 15", "TSLA", null, "touch", "absolute", 250, "2026-11-15");
        yield return Row("SPY below 500 by year end", "SPY", "below", "close", "absolute", 500, "2026-12-31");
        yield return Row("Does Microsoft touch $450 in 30 days?", "MSFT", null, "touch", "absolute", 450, "2026-11-06");
        yield return Row("AAPL close above 250 on Dec 31, 2026", "AAPL", "above", "close", "absolute", 250, "2026-12-31");
        yield return Row("Will the S&P 500 drop 10% by end of the month?", "SPY", "below", "close", "percent", 10, "2026-10-31");
        yield return Row("NVDA over 200 by 12/18/2026", "NVDA", "above", "close", "absolute", 200, "2026-12-18");
        yield return Row("Chance QQQ falls 2% next week", "QQQ", "below", "close", "percent", 2, "2026-10-14");
        yield return Row("TSLA closes above 280 by Friday", "TSLA", "above", "close", "absolute", 280, "2026-10-09");
        yield return Row("$AMD reach 200 before Jan 15 2027", "AMD", null, "touch", "absolute", 200, "2027-01-15");
        yield return Row("Apple gains 5% in 14 days", "AAPL", "above", "close", "percent", 5, "2026-10-21");
        yield return Row("MSFT under $380 this Friday", "MSFT", "below", "close", "absolute", 380, "2026-10-09");
        yield return Row("Does NVDA hit 160 by next Monday?", "NVDA", null, "touch", "absolute", 160, "2026-10-12");
        yield return Row("SPY close above 600 by end of year", "SPY", "above", "close", "absolute", 600, "2026-12-31");
        yield return Row("Will Tesla drop 8% this month?", "TSLA", "below", "close", "percent", 8, "2026-10-31");
        yield return Row("QQQ touch 500 before Dec 1", "QQQ", null, "touch", "absolute", 500, "2026-12-01");
        yield return Row("Invesco QQQ above 520 in 5 days", "QQQ", "above", "close", "absolute", 520, "2026-10-12");
        yield return Row("Will nvidia close below 140 by 2026-11-01?", "NVDA", "below", "close", "absolute", 140, "2026-11-01");
        yield return Row("Microsoft rises 4% by next week", "MSFT", "above", "close", "percent", 4, "2026-10-14");
        yield return Row("AAPL touches 180 before Friday", "AAPL", null, "touch", "absolute", 180, "2026-10-09");
        yield return Row("Chance spy gains 1% in 3 days", "SPY", "above", "close", "percent", 1, "2026-10-10");
        yield return Row("TSLA below $200 by end of month", "TSLA", "below", "close", "absolute", 200, "2026-10-31");
        yield return Row("Will NVDA be above 175 in 21 days?", "NVDA", "above", "close", "absolute", 175, "2026-10-28");
        yield return Row("Does Apple close over $230 by December 20?", "AAPL", "above", "close", "absolute", 230, "2026-12-20");
    }

    [Theory]
    [MemberData(nameof(Questions))]
    public void Parses_varied_questions(
        string question,
        string ticker,
        string? direction,
        string style,
        string levelMode,
        double level,
        string expiry)
    {
        var outcome = _parser.Parse(question, AsOf);
        Assert.Null(outcome.Error);
        var intent = Assert.IsType<ParsedIntent>(outcome.Intent);
        Assert.Equal(ticker, intent.Ticker);
        Assert.Equal(direction, intent.Direction?.ToString().ToLowerInvariant());
        Assert.Equal(style, intent.Style.ToString().ToLowerInvariant());
        Assert.Equal(levelMode, intent.LevelMode.ToString().ToLowerInvariant());
        Assert.Equal(level, intent.Level);
        Assert.Equal(DateOnly.Parse(expiry), intent.Expiry);
        Assert.False(intent.ExpiryAssumed);
        Assert.Equal("rule-based", outcome.ParserName);
        Assert.False(outcome.FellBack);
    }

    [Fact]
    public void Assumes_thirty_days_when_no_date_is_given()
    {
        var outcome = _parser.Parse("NVDA above 100", AsOf);
        var intent = Assert.IsType<ParsedIntent>(outcome.Intent);
        Assert.True(intent.ExpiryAssumed);
        Assert.Equal(new DateOnly(2026, 11, 6), intent.Expiry);
        Assert.Equal(BarrierDirection.Above, intent.Direction);
    }

    [Fact]
    public void Rejects_a_question_with_no_ticker()
    {
        var outcome = _parser.Parse("Will it rain tomorrow?", AsOf);
        Assert.Null(outcome.Intent);
        Assert.Contains("ticker", outcome.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_a_question_with_no_level()
    {
        var outcome = _parser.Parse("Will NVDA close higher by Friday?", AsOf);
        Assert.Null(outcome.Intent);
        Assert.Contains("price", outcome.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static object[] Row(
        string question,
        string ticker,
        string? direction,
        string style,
        string levelMode,
        double level,
        string expiry) =>
        [question, ticker, direction!, style, levelMode, level, expiry];
}
