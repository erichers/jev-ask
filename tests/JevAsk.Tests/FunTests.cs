using System.Text.Json;
using JevAsk.Api.Data;
using JevAsk.Api.Fun;
using JevAsk.Core.Fun;

namespace JevAsk.Tests;

public class FunTests
{
    [Fact]
    public void Vibe_is_deterministic_for_the_same_wording()
    {
        var first = VibeEngine.Evaluate("Will a penguin DJ a wedding?");
        var second = VibeEngine.Evaluate("  will a penguin dj a wedding? ");
        Assert.Equal(first, second);
        Assert.InRange(first.Percent, 2, 97);
        Assert.DoesNotContain("\u2014", first.Reasoning, StringComparison.Ordinal);
    }

    [Fact]
    public void Vibe_hash_matches_fnv_1a_64()
    {
        Assert.Equal(14695981039346656037UL, VibeEngine.Fnv1a64(ReadOnlySpan<byte>.Empty));
        Assert.Equal(0xaf63dc4c8601ec8cUL, VibeEngine.Fnv1a64("a"u8));
    }

    [Fact]
    public void Ever_raises_the_base_and_tomorrow_lowers_it()
    {
        var wider = VibeEngine.Evaluate("Will it ever rain on a parade?");
        Assert.True(wider.Percent > wider.BasePercent);
        Assert.Contains("ever widens", wider.Reasoning, StringComparison.Ordinal);

        var sooner = VibeEngine.Evaluate("Will it rain tomorrow?");
        Assert.True(sooner.Percent < sooner.BasePercent);
        Assert.Contains("tomorrow shrinks", sooner.Reasoning, StringComparison.Ordinal);
    }

    [Fact]
    public void Cat_changes_the_number_and_nearby_words_do_not()
    {
        var cat = VibeEngine.Evaluate("Will the cat nap?");
        Assert.NotEqual(cat.BasePercent, cat.Percent);
        Assert.Contains("cat adds", cat.Reasoning, StringComparison.Ordinal);

        var every = VibeEngine.Evaluate("Will every bus arrive?");
        Assert.Equal(every.BasePercent, every.Percent);

        var category = VibeEngine.Evaluate("Which category fits this?");
        Assert.Equal(category.BasePercent, category.Percent);
    }

    [Fact]
    public void Matcher_hits_exact_and_near_questions_and_misses_unrelated_ones()
    {
        var bank = new[]
        {
            new FunEntry("Will a cat knock a glass off the counter?", 72, "Tables lose.", "Pets", "Base rate"),
            new FunEntry("Will the home side score first?", 56, "Home has a small edge.", "Sports banter", "Base rate")
        };

        var exact = FunMatcher.Match("Will a cat knock a glass off the counter?", bank);
        Assert.NotNull(exact);
        Assert.Equal(72, exact!.Likelihood);

        var near = FunMatcher.Match("will a cat knock a glass off a counter", bank);
        Assert.Equal(exact.Question, near?.Question);

        Assert.Null(FunMatcher.Match("What is the closing price of copper?", bank));
        Assert.Null(FunMatcher.Match("Will a dog sit in a box?", bank));
    }

    [Fact]
    public void Question_bank_has_every_category_and_at_least_150_cards()
    {
        var path = BankPath();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var rows = doc.RootElement.EnumerateArray().ToList();
        Assert.True(rows.Count >= 150, path);
        var categories = rows.Select(row => row.GetProperty("category").GetString()).ToHashSet();
        foreach (var name in new[]
                 {
                     "Pop culture", "Memes and internet", "Movies and TV", "Music",
                     "Sports banter", "Food", "Everyday life", "Space and science", "Pets"
                 })
        {
            Assert.Contains(name, categories);
        }

        foreach (var row in rows)
        {
            var likelihood = row.GetProperty("likelihood").GetInt32();
            Assert.InRange(likelihood, 1, 99);
            var reasoning = row.GetProperty("reasoning").GetString() ?? "";
            Assert.DoesNotContain("\u2014", reasoning, StringComparison.Ordinal);
            Assert.DoesNotContain("\u2013", reasoning, StringComparison.Ordinal);
            Assert.InRange(reasoning.Count(ch => ch is '.' or '!' or '?'), 2, 3);
            var tag = row.GetProperty("tag").GetString();
            Assert.Contains(tag, new[] { "Base rate", "Vibes", "Physics says no" });
        }
    }

    [Fact]
    public void Ollama_cleaner_keeps_a_short_reason_and_drops_a_fragment()
    {
        Assert.Null(FunOllama.Clean("Too short."));
        Assert.Null(FunOllama.Clean(null));
        var kept = FunOllama.Clean("The hashed answer stays put.\nSame words, same number.");
        Assert.Equal("The hashed answer stays put. Same words, same number.", kept);
    }

    [Fact]
    public void Public_fun_links_follow_the_base_url()
    {
        Assert.Equal(
            "http://localhost:8888/grokbot/asp/jev-ask/fun/4",
            PublicLinks.Fun("http://localhost:8888/grokbot/asp/jev-ask/", 4));
        Assert.Null(PublicLinks.Fun("", 4));
    }

    private static string BankPath()
    {
        var direct = Path.Combine(AppContext.BaseDirectory, "data", "fun", "questions.json");
        if (File.Exists(direct))
            return direct;
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "fun", "questions.json"));
    }
}
