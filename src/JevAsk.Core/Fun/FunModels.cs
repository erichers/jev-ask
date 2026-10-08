namespace JevAsk.Core.Fun;

public sealed record FunEntry(string Question, int Likelihood, string Reasoning, string Category, string Tag);

public sealed record FunAnswer
{
    public const string DisclaimerText = "For fun. Not a prediction.";

    public long Id { get; init; }
    public string Question { get; init; } = "";
    public int Percent { get; init; }
    public double Likelihood { get; init; }
    public string Reasoning { get; init; } = "";
    public string Category { get; init; } = "";
    public string Tag { get; init; } = "";
    public string Source { get; init; } = "";
    public string Disclaimer { get; init; } = DisclaimerText;
    public string? Url { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public readonly record struct VibeResult(int BasePercent, int Percent, string Reasoning, ulong Hash);
