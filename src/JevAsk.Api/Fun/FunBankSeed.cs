using System.Text.Json;
using JevAsk.Api.Data;
using JevAsk.Api.Market;
using JevAsk.Core.Fun;
using Microsoft.EntityFrameworkCore;

namespace JevAsk.Api.Fun;

public static class FunBankSeed
{
    public static async Task SeedAsync(CacheDb db, string path, CancellationToken cancellationToken)
    {
        var entries = Load(path);
        if (entries.Count == 0)
            return;

        var existing = await db.FunCards.Select(row => row.Question).ToListAsync(cancellationToken);
        var have = new HashSet<string>(existing.Select(FunText.Normalize), StringComparer.Ordinal);
        var added = false;
        foreach (var entry in entries)
        {
            if (!have.Add(FunText.Normalize(entry.Question)))
                continue;
            db.FunCards.Add(new FunCard
            {
                Question = entry.Question,
                Likelihood = entry.Likelihood,
                Reasoning = entry.Reasoning,
                Category = entry.Category,
                Tag = entry.Tag
            });
            added = true;
        }

        if (added)
            await db.SaveChangesAsync(cancellationToken);
    }

    public static List<FunEntry> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return [];
        var rows = JsonSerializer.Deserialize<List<FunFileRow>>(File.ReadAllText(path), AppJson.Options) ?? [];
        var entries = new List<FunEntry>();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Question) || string.IsNullOrWhiteSpace(row.Reasoning))
                continue;
            if (string.IsNullOrWhiteSpace(row.Category) || string.IsNullOrWhiteSpace(row.Tag))
                continue;
            entries.Add(new FunEntry(
                row.Question.Trim(),
                row.Likelihood,
                row.Reasoning.Trim(),
                row.Category.Trim(),
                row.Tag.Trim()));
        }

        return entries;
    }

    public static string ResolvePath(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["Data:FunPath"];
        var candidates = new[]
        {
            configured,
            Path.Combine(env.ContentRootPath, "data", "fun", "questions.json"),
            Path.Combine(AppContext.BaseDirectory, "data", "fun", "questions.json"),
            Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "data", "fun", "questions.json"))
        };
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                return candidate;
        }

        return Path.Combine(AppContext.BaseDirectory, "data", "fun", "questions.json");
    }

    private sealed class FunFileRow
    {
        public string? Question { get; set; }
        public int Likelihood { get; set; }
        public string? Reasoning { get; set; }
        public string? Category { get; set; }
        public string? Tag { get; set; }
    }
}
