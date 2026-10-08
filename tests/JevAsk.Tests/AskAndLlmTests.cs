using System.Net;
using System.Text;
using System.Text.Json;
using JevAsk.Api.Brief;
using JevAsk.Api.Data;
using JevAsk.Api.Fun;
using JevAsk.Api.Market;
using JevAsk.Api.Parsing;
using JevAsk.Core.Ask;
using JevAsk.Core.Parsing;
using JevAsk.Core.Probability;
using Microsoft.EntityFrameworkCore;

namespace JevAsk.Tests;

public class AskAndLlmTests
{
    [Fact]
    public async Task Ask_uses_the_rule_parser_and_reports_cached_data()
    {
        var bars = BuildBars();
        var market = new FakeMarket(bars);
        var service = new AskService(new RuleBasedParser(), market, new ProbabilityEngine(), 800, 21);
        var response = await service.AskAsync("Will NVDA close above 150 by Friday?", new DateOnly(2026, 10, 7), CancellationToken.None);

        Assert.Equal("NVDA", response.Intent.Ticker);
        Assert.Equal(BarrierDirection.Above, response.Intent.Direction);
        Assert.Equal("rule-based", response.Parser);
        Assert.False(response.ParserFellBack);
        Assert.Equal("cached", response.DataFreshness);
        Assert.InRange(response.Probability, 0, 1);
        Assert.InRange(response.Probability, response.BandLow, response.BandHigh);
        Assert.Equal(5, response.Steps.Count);
        Assert.Equal(ProductCopy.Disclaimer, response.Disclaimer);
        Assert.DoesNotContain("\u2014", response.Reasoning, StringComparison.Ordinal);
        Assert.DoesNotContain("\u2014", string.Join(" ", response.Steps.Select(step => step.Detail)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Recompute_marks_a_manual_edit()
    {
        var service = new AskService(new RuleBasedParser(), new FakeMarket(BuildBars()), new ProbabilityEngine(), 400, 2);
        var response = await service.RecomputeAsync(
            "spy", "below", "close", "percent", 5, new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 7), CancellationToken.None);
        Assert.True(response.ManualEdit);
        Assert.Equal("manual-edit", response.Parser);
        Assert.Equal("SPY", response.Intent.Ticker);
        Assert.Equal(LevelMode.Percent, response.Intent.LevelMode);
        Assert.True(response.TargetPrice < response.Spot);
    }

    [Fact]
    public void Valid_model_json_is_accepted()
    {
        const string json = """
            {"ticker":"NVDA","condition":"above","style":"close","levelMode":"absolute","level":150,"expiry":"2026-10-09"}
            """;
        var ok = IntentJson.TryParse("```json\n" + json + "\n```", "Will NVDA close above 150 by Friday?", out var intent, out var error);
        Assert.True(ok, error);
        Assert.Equal("NVDA", intent!.Ticker);
        Assert.Equal(new DateOnly(2026, 10, 9), intent.Expiry);
    }

    [Theory]
    [InlineData("{\"condition\":\"above\"}")]
    [InlineData("{\"ticker\":\"NVDA\",\"condition\":\"sideways\",\"style\":\"close\",\"levelMode\":\"absolute\",\"level\":1,\"expiry\":\"2026-10-09\"}")]
    [InlineData("{\"ticker\":\"NVDA\",\"condition\":\"above\",\"style\":\"close\",\"levelMode\":\"absolute\",\"level\":0,\"expiry\":\"2026-10-09\"}")]
    [InlineData("not json")]
    public void Invalid_model_json_is_rejected(string content)
    {
        var ok = IntentJson.TryParse(content, "q", out _, out var error);
        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task Llm_success_keeps_the_model_name()
    {
        var handler = new StubHandler("""
            {"choices":[{"message":{"content":"{\"ticker\":\"MSFT\",\"condition\":\"below\",\"style\":\"close\",\"levelMode\":\"absolute\",\"level\":400,\"expiry\":\"2026-10-16\"}"}}]}
            """);
        var settings = new LlmSettings("groq", "groq:llama-3.1-8b-instant", "https://api.groq.com/openai/v1/chat/completions", "llama-3.1-8b-instant", "test-key");
        var parser = new LlmQuestionParser(new HttpClient(handler), settings, new RuleBasedParser());
        var outcome = await parser.ParseAsync("Will MSFT close under 400 by next Friday?", new DateOnly(2026, 10, 7), CancellationToken.None);
        Assert.False(outcome.FellBack);
        Assert.Equal("groq:llama-3.1-8b-instant", outcome.ParserName);
        Assert.Equal("MSFT", outcome.Intent!.Ticker);
        Assert.NotNull(handler.Last);
        Assert.Equal(HttpMethod.Post, handler.Last!.Method);
        Assert.Equal("Bearer test-key", handler.Last.Headers.Authorization?.ToString());
    }

    [Fact]
    public async Task Llm_failure_falls_back_to_the_rule_parser()
    {
        var handler = new StubHandler("""{"choices":[{"message":{"content":"I cannot help"}}]}""");
        var settings = new LlmSettings("openrouter", "openrouter:free", "https://openrouter.ai/api/v1/chat/completions", "free", "test-key");
        var parser = new LlmQuestionParser(new HttpClient(handler), settings, new RuleBasedParser());
        var outcome = await parser.ParseAsync("Will NVDA close above 150 by Friday?", new DateOnly(2026, 10, 7), CancellationToken.None);
        Assert.True(outcome.FellBack);
        Assert.Equal("rule-based", outcome.ParserName);
        Assert.Equal("openrouter:free", outcome.AttemptedParser);
        Assert.Equal("NVDA", outcome.Intent!.Ticker);
        Assert.Equal(new DateOnly(2026, 10, 9), outcome.Intent.Expiry);
    }

    [Fact]
    public async Task Missing_key_falls_back_without_a_request()
    {
        var handler = new StubHandler("{}");
        var settings = new LlmSettings("gemini", "gemini:gemini-2.0-flash", "https://generativelanguage.googleapis.com/v1beta/models/", "gemini-2.0-flash", "");
        var parser = new LlmQuestionParser(new HttpClient(handler), settings, new RuleBasedParser());
        var outcome = await parser.ParseAsync("QQQ rises 3% in 10 days", new DateOnly(2026, 10, 7), CancellationToken.None);
        Assert.True(outcome.FellBack);
        Assert.Equal("QQQ", outcome.Intent!.Ticker);
        Assert.Null(handler.Last);
    }

    [Fact]
    public async Task Ollama_and_gemini_shapes_are_read()
    {
        var intent = """{"ticker":"AAPL","condition":"above","style":"touch","levelMode":"absolute","level":180,"expiry":"2026-10-09"}""";
        var ollama = new StubHandler(JsonSerializer.Serialize(new { message = new { content = intent } }));
        var ollamaParser = new LlmQuestionParser(
            new HttpClient(ollama),
            new LlmSettings("ollama", "ollama:llama3.1", "http://localhost:11434", "llama3.1", ""),
            new RuleBasedParser());
        var ollamaOutcome = await ollamaParser.ParseAsync("AAPL touches 180 before Friday", new DateOnly(2026, 10, 7), CancellationToken.None);
        Assert.False(ollamaOutcome.FellBack);
        Assert.EndsWith("/api/chat", ollama.Last!.RequestUri!.AbsolutePath, StringComparison.Ordinal);

        var geminiBody = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text = intent } } } }
            }
        });
        var gemini = new StubHandler(geminiBody);
        var geminiParser = new LlmQuestionParser(
            new HttpClient(gemini),
            new LlmSettings("gemini", "gemini:gemini-2.0-flash", "https://generativelanguage.googleapis.com/v1beta/models/", "gemini-2.0-flash", "test-key"),
            new RuleBasedParser());
        var geminiOutcome = await geminiParser.ParseAsync("AAPL touches 180 before Friday", new DateOnly(2026, 10, 7), CancellationToken.None);
        Assert.False(geminiOutcome.FellBack);
        Assert.Contains("generateContent", gemini.Last!.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
        Assert.Contains("test-key", gemini.Last.RequestUri.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pdf_brief_is_a_pdf()
    {
        var service = new AskService(new RuleBasedParser(), new FakeMarket(BuildBars()), new ProbabilityEngine(), 400, 2);
        var response = await service.AskAsync("Chance SPY drops 5% this month?", new DateOnly(2026, 10, 7), CancellationToken.None);
        var bytes = PdfBrief.Render(response);
        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Bundled_samples_cover_the_demo_tickers()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "data", "samples");
        if (!Directory.Exists(dir))
            dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "samples"));

        foreach (var ticker in DemoCatalog.Tickers)
        {
            var path = Path.Combine(dir, ticker + ".json");
            Assert.True(File.Exists(path), path);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var count = doc.RootElement.GetProperty("bars").GetArrayLength();
            Assert.True(count > 200, ticker);
        }
    }

    [Fact]
    public void Public_links_use_the_configured_base_and_skip_when_unset()
    {
        Assert.Equal(
            "http://localhost:8888/grokbot/asp/jev-ask/q/4",
            PublicLinks.Question("http://localhost:8888/grokbot/asp/jev-ask/", 4));
        Assert.Null(PublicLinks.Question("", 4));
        Assert.Null(PublicLinks.Question("http://localhost:8888/grokbot/asp/jev-ask", 0));
    }

    [Fact]
    public void Mysql_schema_stays_compatible_with_5_7()
    {
        var sql = MySqlSchema.CreateQuestions + MySqlSchema.CreateSeries + MySqlSchema.CreateQuestionsIndex
            + MySqlSchema.CreateFunCards + MySqlSchema.CreateFunAsks
            + MySqlSchema.CreateFunCardsIndex + MySqlSchema.CreateFunAsksIndex;
        Assert.Contains("utf8mb4_unicode_ci", sql, StringComparison.Ordinal);
        Assert.Contains("longtext", sql, StringComparison.Ordinal);
        Assert.Contains("datetime(6)", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("utf8mb4_0900", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("INVISIBLE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sqlite_migration_stores_question_history()
    {
        var path = Path.Combine(Path.GetTempPath(), "jev-ask-" + Guid.NewGuid().ToString("N") + ".db");
        var options = new DbContextOptionsBuilder<CacheDb>().UseSqlite("Data Source=" + path).Options;
        await using var db = new CacheDb(options);
        await db.Database.MigrateAsync();
        db.Questions.Add(new AskedQuestion
        {
            Question = "Will SPY close above 100 by Friday?",
            Ticker = "SPY",
            Probability = 0.12,
            PayloadJson = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        Assert.Equal(1, await db.Questions.CountAsync());
        Assert.True(await db.Series.CountAsync() == 0);
        db.FunAsks.Add(new FunAskRow
        {
            Question = "Will a cat sit in a box?",
            Likelihood = 0.91,
            Reasoning = "Boxes win.",
            Category = "Pets",
            Tag = "Base rate",
            Source = "bank",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        Assert.Equal(1, await db.FunAsks.CountAsync());
        File.Delete(path);
    }

    private static List<DailyBar> BuildBars()
    {
        var price = 140.0;
        var day = new DateOnly(2025, 1, 2);
        var rng = new Random(3);
        var bars = new List<DailyBar>();
        while (bars.Count < 320)
        {
            if (day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                bars.Add(new DailyBar(day, price));
                price *= 1 + (rng.NextDouble() - 0.48) * 0.02;
            }

            day = day.AddDays(1);
        }

        return bars;
    }

    private sealed class FakeMarket : IMarketData
    {
        private readonly IReadOnlyList<DailyBar> _bars;

        public FakeMarket(IReadOnlyList<DailyBar> bars) => _bars = bars;

        public Task<MarketSeries> GetAsync(string ticker, CancellationToken cancellationToken) =>
            Task.FromResult(new MarketSeries(ticker, "cached", "bundled sample", _bars));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;

        public StubHandler(string body) => _body = body;

        public HttpRequestMessage? Last { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Last = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
