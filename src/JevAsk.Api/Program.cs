using System.Text.Json;
using System.Text.Json.Serialization;
using JevAsk.Api.Brief;
using JevAsk.Api.Data;
using JevAsk.Api.Market;
using JevAsk.Api.Parsing;
using JevAsk.Core.Ask;
using JevAsk.Core.Parsing;
using JevAsk.Core.Probability;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var databaseProvider = First(
    Environment.GetEnvironmentVariable("DATABASE_PROVIDER"),
    builder.Configuration["Database:Provider"],
    "sqlite");
var mysqlConnection = First(
    Environment.GetEnvironmentVariable("MYSQL_CONNECTION"),
    builder.Configuration.GetConnectionString("MySql"));
if (databaseProvider.Equals("mysql", StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(mysqlConnection))
        throw new InvalidOperationException("DATABASE_PROVIDER is mysql, but MYSQL_CONNECTION is empty.");
    builder.Services.AddDbContext<CacheDb>(options =>
        options.UseMySql(mysqlConnection, new MySqlServerVersion(new Version(8, 4, 0))));
}
else
{
    var cacheConnection = SqliteConnection.Normalize(
        builder.Configuration.GetConnectionString("Cache"),
        builder.Environment.ContentRootPath);
    builder.Services.AddDbContext<CacheDb>(options => options.UseSqlite(cacheConnection));
    databaseProvider = "sqlite";
}
builder.Services.AddHttpClient("market", client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("JevAsk/1.0 (educational research; no-key market read)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("llm", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddSingleton<RuleBasedParser>();
builder.Services.AddSingleton<ProbabilityEngine>();
builder.Services.AddSingleton<IQuestionParser, SelectingParser>();
builder.Services.AddScoped<IMarketData, MarketDataService>();
builder.Services.AddScoped<QuestionHistory>();
builder.Services.AddScoped(sp =>
{
    var paths = sp.GetRequiredService<IConfiguration>().GetValue("MonteCarlo:Paths", 4000);
    var seed = sp.GetRequiredService<IConfiguration>().GetValue("MonteCarlo:Seed", 20261008);
    return new AskService(
        sp.GetRequiredService<IQuestionParser>(),
        sp.GetRequiredService<IMarketData>(),
        sp.GetRequiredService<ProbabilityEngine>(),
        paths,
        seed);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CacheDb>();
    var samplePath = MarketDataService.ResolveSamplePath(app.Environment, app.Configuration);
    await DatabaseBootstrap.PrepareAsync(db, samplePath, CancellationToken.None);
}

app.UseCors();

app.MapGet("/api/health", (IQuestionParser parser) => Results.Ok(new
{
    status = "ok",
    parser = parser.Name,
    database = databaseProvider
}));

app.MapGet("/api/examples", () => Results.Ok(ExampleQuestions.All));

app.MapGet("/api/tape", async (IMarketData market, CancellationToken ct) =>
{
    var symbols = new[] { "SPY", "QQQ", "NVDA", "AAPL", "TSLA", "MSFT", "AMZN", "META" };
    var rows = new List<object>();
    foreach (var symbol in symbols)
    {
        try
        {
            var series = await market.GetAsync(symbol, ct);
            var last = series.Bars[^1];
            var prev = series.Bars.Count > 1 ? series.Bars[^2].Close : last.Close;
            var change = prev == 0 ? 0 : (last.Close - prev) / prev;
            rows.Add(new
            {
                ticker = symbol,
                close = last.Close,
                date = last.Date,
                change,
                freshness = series.Freshness,
                origin = series.Origin
            });
        }
        catch (AskException)
        {
            // Skip a symbol that has no bundled or live history.
        }
    }

    return Results.Ok(rows);
});

app.MapGet("/api/history", async (QuestionHistory history, CancellationToken ct) =>
    Results.Ok(await history.ListAsync(ct)));

app.MapGet("/api/history/{id:long}", async (long id, QuestionHistory history, CancellationToken ct) =>
{
    var row = await history.FindAsync(id, ct);
    return row is null ? Results.NotFound(new { error = "That question is not in the history." }) : Results.Ok(row);
});

app.MapPost("/api/ask", async (AskBody body, AskService ask, QuestionHistory history, CancellationToken ct) =>
{
    try
    {
        var asOf = body.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await ask.AskAsync(body.Question, asOf, ct);
        return Results.Ok(await history.SaveAsync(response, ct));
    }
    catch (AskException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/recompute", async (RecomputeBody body, AskService ask, QuestionHistory history, CancellationToken ct) =>
{
    try
    {
        var asOf = body.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await ask.RecomputeAsync(
            body.Ticker, body.Condition, body.Style, body.LevelMode, body.Level, body.Expiry, asOf, ct);
        return Results.Ok(await history.SaveAsync(response, ct));
    }
    catch (AskException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/brief.pdf", (AskResponse response) =>
{
    if (string.IsNullOrWhiteSpace(response.Question) || response.Steps is null || response.Steps.Count == 0)
        return Results.BadRequest(new { error = "Nothing to export yet." });
    var pdf = PdfBrief.Render(response);
    return Results.File(pdf, "application/pdf", "jev-ask.pdf");
});

app.Run();

static string First(params string?[] values) =>
    values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";

public sealed record AskBody(string? Question, DateOnly? AsOf);

public sealed record RecomputeBody(
    string? Ticker,
    string? Condition,
    string? Style,
    string? LevelMode,
    double? Level,
    DateOnly? Expiry,
    DateOnly? AsOf);

public partial class Program;
