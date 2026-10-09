namespace JevAsk.Core.Parsing;

public enum BarrierDirection
{
    Above,
    Below
}

public enum BarrierStyle
{
    Close,
    Touch
}

public enum LevelMode
{
    Absolute,
    Percent
}

public sealed record ParsedIntent(
    string Ticker,
    BarrierDirection? Direction,
    BarrierStyle Style,
    LevelMode LevelMode,
    double Level,
    DateOnly Expiry,
    bool ExpiryAssumed,
    string RawText);

public sealed record ParseOutcome(
    ParsedIntent? Intent,
    string? Error,
    string ParserName,
    bool FellBack,
    string? AttemptedParser)
{
    public static ParseOutcome Ok(ParsedIntent intent, string name) =>
        new(intent, null, name, false, null);

    public static ParseOutcome Fail(string error, string name) =>
        new(null, error, name, false, null);
}

public interface IQuestionParser
{
    string Name { get; }

    Task<ParseOutcome> ParseAsync(string question, DateOnly asOf, CancellationToken cancellationToken);
}
