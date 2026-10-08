namespace JevAsk.Core.Fun;

public static class FunMatcher
{
    public const double Threshold = 0.55;

    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "a", "an", "the", "of", "to", "in", "on", "for", "and", "or",
        "will", "would", "could", "does", "do", "is", "are", "be", "it",
        "this", "that", "at", "by", "with", "from", "into", "over",
        "your", "you", "my", "our", "if", "than", "then", "just"
    };

    public static FunEntry? Match(string? question, IReadOnlyList<FunEntry> bank)
    {
        var tokens = ContentTokens(question);
        if (tokens.Count == 0 || bank.Count == 0)
            return null;

        FunEntry? best = null;
        var bestScore = 0.0;
        foreach (var entry in bank)
        {
            var other = ContentTokens(entry.Question);
            if (other.Count == 0)
                continue;
            var score = Score(tokens, other);
            if (score > bestScore)
            {
                bestScore = score;
                best = entry;
            }
        }

        return bestScore >= Threshold ? best : null;
    }

    public static double Score(IReadOnlySet<string> left, IReadOnlySet<string> right)
    {
        var intersection = 0;
        foreach (var token in left)
        {
            if (right.Contains(token))
                intersection++;
        }

        var union = left.Count + right.Count - intersection;
        var jaccard = union == 0 ? 0 : intersection / (double)union;
        var shorter = Math.Min(left.Count, right.Count);
        var smallerIsSubset = intersection == shorter;
        if (smallerIsSubset && shorter >= 3)
            jaccard = Math.Max(jaccard, 0.72);
        return jaccard;
    }

    public static HashSet<string> ContentTokens(string? question)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in FunText.Tokens(FunText.Normalize(question)))
        {
            if (!Stopwords.Contains(token))
                set.Add(token);
        }

        return set;
    }
}
