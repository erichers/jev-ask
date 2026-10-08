using System.Text;

namespace JevAsk.Core.Fun;

public static class VibeEngine
{
    public static VibeResult Evaluate(string? question)
    {
        var normalized = FunText.Normalize(question);
        var hash = Fnv1a64(Encoding.UTF8.GetBytes(normalized));
        var basePercent = 6 + (int)(hash % 89);
        var adjusted = basePercent;
        var words = new HashSet<string>(FunText.Tokens(normalized), StringComparer.Ordinal);
        if (words.Contains("ever"))
            adjusted += 18;
        if (words.Contains("tomorrow"))
            adjusted -= 22;
        if (words.Contains("cat") || words.Contains("cats"))
        {
            var swing = (int)((hash >> 12) % 21) - 7;
            if (swing == 0)
                swing = 9;
            adjusted += swing;
        }

        var percent = Math.Clamp(adjusted, 2, 97);
        return new VibeResult(basePercent, percent, Reason(percent, words), hash);
    }

    public static ulong Fnv1a64(ReadOnlySpan<byte> data)
    {
        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;
        var hash = offset;
        foreach (var b in data)
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }

    private static string Reason(int percent, HashSet<string> words)
    {
        var band = percent switch
        {
            < 20 => "a long shot",
            < 40 => "uncommon",
            < 60 => "a coin-flip neighborhood",
            < 80 => "more likely than not",
            _ => "the house favorite"
        };
        var notes = new List<string>();
        if (words.Contains("ever"))
            notes.Add("ever widens the window");
        if (words.Contains("tomorrow"))
            notes.Add("tomorrow shrinks it");
        if (words.Contains("cat") || words.Contains("cats"))
            notes.Add("a cat adds a fixed wobble");
        var third = notes.Count == 0
            ? "No keyword rule moved the base hash."
            : "Keyword rules applied: " + string.Join(", ", notes) + ".";
        return "The vibe engine hashed the wording and landed on " + percent +
               " percent, which reads as " + band +
               ". The same question always returns this number. " + third;
    }
}
