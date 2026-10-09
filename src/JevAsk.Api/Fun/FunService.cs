using JevAsk.Api.Market;
using JevAsk.Core.Fun;
using Microsoft.EntityFrameworkCore;

namespace JevAsk.Api.Fun;

public sealed class FunService
{
    private readonly CacheDb _db;
    private readonly IHttpClientFactory _http;

    public FunService(CacheDb db, IHttpClientFactory http)
    {
        _db = db;
        _http = http;
    }

    public async Task<FunAnswer> AskAsync(string? question, CancellationToken cancellationToken)
    {
        var text = (question ?? "").Trim();
        if (text.Length == 0)
            throw new FunException("Type a fun question first.");
        if (text.Length > 500)
            text = text[..500];

        var bank = await EntriesAsync(cancellationToken);
        var match = FunMatcher.Match(text, bank);
        if (match is not null)
            return await SaveAsync(FromEntry(match, "bank"), cancellationToken);

        var vibe = VibeEngine.Evaluate(text);
        var reasoning = vibe.Reasoning;
        var source = "vibe";
        var rewritten = await FunOllama.TryReasonAsync(_http.CreateClient("llm"), text, vibe.Percent, cancellationToken);
        if (!string.IsNullOrWhiteSpace(rewritten))
        {
            reasoning = rewritten;
            source = "ollama";
        }

        return await SaveAsync(new FunAnswer
        {
            Question = text,
            Percent = vibe.Percent,
            Likelihood = vibe.Percent / 100.0,
            Reasoning = reasoning,
            Category = "Free text",
            Tag = "Vibes",
            Source = source
        }, cancellationToken);
    }

    public async Task<FunAnswer> ShuffleAsync(CancellationToken cancellationToken)
    {
        var count = await _db.FunCards.CountAsync(cancellationToken);
        if (count == 0)
            throw new FunException("The fun bank is empty.");
        var skip = Random.Shared.Next(count);
        var card = await _db.FunCards.OrderBy(row => row.Id).Skip(skip).FirstAsync(cancellationToken);
        return await SaveAsync(FromCard(card, "bank"), cancellationToken);
    }

    public async Task<IReadOnlyList<FunEntry>> BankAsync(string? category, CancellationToken cancellationToken)
    {
        var query = _db.FunCards.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var name = category.Trim();
            query = query.Where(row => row.Category == name);
        }

        var rows = await query.OrderBy(row => row.Id).ToListAsync(cancellationToken);
        return rows.Select(row => new FunEntry(row.Question, row.Likelihood, row.Reasoning, row.Category, row.Tag)).ToList();
    }

    public async Task<FunAnswer?> FindAsync(long id, CancellationToken cancellationToken)
    {
        var row = await _db.FunAsks.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return row is null ? null : ToAnswer(row);
    }

    public async Task<IReadOnlyList<FunAnswer>> HistoryAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.FunAsks.AsNoTracking()
            .OrderByDescending(row => row.CreatedAtUtc)
            .ThenByDescending(row => row.Id)
            .Take(12)
            .ToListAsync(cancellationToken);
        return rows.Select(ToAnswer).ToList();
    }

    private async Task<IReadOnlyList<FunEntry>> EntriesAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.FunCards.AsNoTracking().ToListAsync(cancellationToken);
        return rows.Select(row => new FunEntry(row.Question, row.Likelihood, row.Reasoning, row.Category, row.Tag)).ToList();
    }

    private async Task<FunAnswer> SaveAsync(FunAnswer answer, CancellationToken cancellationToken)
    {
        var row = new FunAskRow
        {
            Question = answer.Question,
            Likelihood = answer.Likelihood,
            Reasoning = answer.Reasoning,
            Category = answer.Category,
            Tag = answer.Tag,
            Source = answer.Source,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.FunAsks.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return answer with { Id = row.Id, CreatedAtUtc = row.CreatedAtUtc };
    }

    private static FunAnswer FromCard(FunCard card, string source) =>
        FromEntry(new FunEntry(card.Question, card.Likelihood, card.Reasoning, card.Category, card.Tag), source);

    private static FunAnswer FromEntry(FunEntry entry, string source) => new()
    {
        Question = entry.Question,
        Percent = entry.Likelihood,
        Likelihood = entry.Likelihood / 100.0,
        Reasoning = entry.Reasoning,
        Category = entry.Category,
        Tag = entry.Tag,
        Source = source
    };

    private static FunAnswer ToAnswer(FunAskRow row) => new()
    {
        Id = row.Id,
        Question = row.Question,
        Percent = (int)Math.Round(row.Likelihood * 100),
        Likelihood = row.Likelihood,
        Reasoning = row.Reasoning,
        Category = row.Category,
        Tag = row.Tag,
        Source = row.Source,
        CreatedAtUtc = row.CreatedAtUtc
    };
}
