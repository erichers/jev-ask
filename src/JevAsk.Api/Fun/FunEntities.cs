namespace JevAsk.Api.Fun;

public sealed class FunCard
{
    public long Id { get; set; }
    public string Question { get; set; } = "";
    public int Likelihood { get; set; }
    public string Reasoning { get; set; } = "";
    public string Category { get; set; } = "";
    public string Tag { get; set; } = "";
}

public sealed class FunAskRow
{
    public long Id { get; set; }
    public string Question { get; set; } = "";
    public double Likelihood { get; set; }
    public string Reasoning { get; set; } = "";
    public string Category { get; set; } = "";
    public string Tag { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class FunException : Exception
{
    public FunException(string message) : base(message)
    {
    }
}
