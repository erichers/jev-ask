using System.Globalization;
using System.Text.Json;
using JevAsk.Api.Fun;
using JevAsk.Api.Market;
using JevAsk.Core.Probability;
using Microsoft.EntityFrameworkCore;

namespace JevAsk.Api.Data;

public static class DatabaseBootstrap
{
    public static async Task PrepareAsync(
        CacheDb db,
        string samplePath,
        CancellationToken cancellationToken,
        string? funBankPath = null)
    {
        await db.Database.MigrateAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(funBankPath))
            await FunBankSeed.SeedAsync(db, funBankPath, cancellationToken);
        if (await db.Series.AnyAsync(cancellationToken))
            return;
        if (!Directory.Exists(samplePath))
            return;

        foreach (var file in Directory.EnumerateFiles(samplePath, "*.json"))
        {
            var ticker = Path.GetFileNameWithoutExtension(file).ToUpperInvariant();
            var bars = ReadBars(file);
            if (bars is null)
                continue;
            db.Series.Add(new CachedSeries
            {
                Ticker = ticker,
                BarsJson = JsonSerializer.Serialize(bars),
                Origin = "bundled sample",
                StoredAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<DailyBar>? ReadBars(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("bars", out var barsEl))
            return null;
        var bars = new List<DailyBar>();
        foreach (var el in barsEl.EnumerateArray())
        {
            if (!el.TryGetProperty("date", out var dateEl) || !el.TryGetProperty("close", out var closeEl))
                continue;
            if (!DateOnly.TryParse(dateEl.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue;
            if (closeEl.ValueKind != JsonValueKind.Number)
                continue;
            bars.Add(new DailyBar(date, closeEl.GetDouble()));
        }

        return bars.Count >= 30 ? bars : null;
    }
}
