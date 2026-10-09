using System.Text.Json;
using JevAsk.Api.Market;
using JevAsk.Core.Ask;
using Microsoft.EntityFrameworkCore;

namespace JevAsk.Api.Data;

public sealed record HistorySummary(long Id, string Question, string Ticker, double Probability, DateTime CreatedAtUtc, string? Url);

public sealed class QuestionHistory
{
    private readonly CacheDb _db;

    public QuestionHistory(CacheDb db) => _db = db;

    public async Task<AskResponse> SaveAsync(AskResponse response, CancellationToken cancellationToken)
    {
        var row = new AskedQuestion
        {
            Question = response.Question.Length > 500 ? response.Question[..500] : response.Question,
            Ticker = response.Intent.Ticker,
            Probability = response.Probability,
            PayloadJson = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Questions.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        var saved = response with { Id = row.Id };
        row.PayloadJson = JsonSerializer.Serialize(saved, AppJson.Options);
        await _db.SaveChangesAsync(cancellationToken);
        return saved;
    }

    public async Task<IReadOnlyList<HistorySummary>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Questions.AsNoTracking()
            .OrderByDescending(row => row.CreatedAtUtc)
            .Take(12)
            .Select(row => new HistorySummary(row.Id, row.Question, row.Ticker, row.Probability, row.CreatedAtUtc, null))
            .ToListAsync(cancellationToken);
    }

    public async Task<AskResponse?> FindAsync(long id, CancellationToken cancellationToken)
    {
        var row = await _db.Questions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (row is null || string.IsNullOrWhiteSpace(row.PayloadJson))
            return null;
        return JsonSerializer.Deserialize<AskResponse>(row.PayloadJson, AppJson.Options);
    }
}
