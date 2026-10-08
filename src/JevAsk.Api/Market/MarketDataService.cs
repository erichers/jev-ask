using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using JevAsk.Core.Ask;
using JevAsk.Core.Probability;
using Microsoft.EntityFrameworkCore;

namespace JevAsk.Api.Market;

public sealed class MarketDataService : IMarketData
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _http;
    private readonly CacheDb _db;
    private readonly string _samplePath;

    public MarketDataService(IHttpClientFactory http, CacheDb db, IWebHostEnvironment env, IConfiguration config)
    {
        _http = http;
        _db = db;
        _samplePath = ResolveSamplePath(env, config);
    }

    public async Task<MarketSeries> GetAsync(string ticker, CancellationToken cancellationToken)
    {
        var symbol = ticker.Trim().ToUpperInvariant();
        var yahoo = await TryYahooAsync(symbol, cancellationToken);
        if (yahoo is not null)
        {
            await SaveAsync(symbol, yahoo, "Yahoo Finance", cancellationToken);
            return new MarketSeries(symbol, "live", "Yahoo Finance", yahoo);
        }

        var stooq = await TryStooqAsync(symbol, cancellationToken);
        if (stooq is not null)
        {
            await SaveAsync(symbol, stooq, "Stooq", cancellationToken);
            return new MarketSeries(symbol, "live", "Stooq", stooq);
        }

        var cached = await ReadCacheAsync(symbol, cancellationToken);
        if (cached is not null)
            return new MarketSeries(symbol, "cached", cached.Value.Origin, cached.Value.Bars);

        var sample = ReadSample(symbol);
        if (sample is not null)
            return new MarketSeries(symbol, "cached", "bundled sample", sample);

        throw new AskException(
            $"No price history for {symbol}. The bundled cache covers the demo tickers when live data is unavailable.");
    }

    public static string ResolveSamplePath(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["Data:SamplePath"];
        var candidates = new[]
        {
            configured,
            Path.Combine(env.ContentRootPath, "data", "samples"),
            Path.Combine(AppContext.BaseDirectory, "data", "samples"),
            Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "data", "samples"))
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
                return candidate;
        }

        return Path.Combine(AppContext.BaseDirectory, "data", "samples");
    }

    private async Task<List<DailyBar>?> TryYahooAsync(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            var client = _http.CreateClient("market");
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?range=5y&interval=1d";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("chart", out var chart)
                || !chart.TryGetProperty("result", out var result)
                || result.ValueKind != JsonValueKind.Array
                || result.GetArrayLength() == 0)
                return null;

            var row = result[0];
            if (!row.TryGetProperty("timestamp", out var timestamps)
                || !row.TryGetProperty("indicators", out var indicators)
                || !indicators.TryGetProperty("quote", out var quote)
                || quote.GetArrayLength() == 0
                || !quote[0].TryGetProperty("close", out var closes))
                return null;

            var bars = new List<DailyBar>();
            var i = 0;
            foreach (var tsEl in timestamps.EnumerateArray())
            {
                double? close = null;
                if (i < closes.GetArrayLength() && closes[i].ValueKind == JsonValueKind.Number)
                    close = closes[i].GetDouble();
                i++;
                if (close is null || tsEl.ValueKind != JsonValueKind.Number)
                    continue;
                var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(tsEl.GetInt64()).UtcDateTime);
                bars.Add(new DailyBar(date, close.Value));
            }

            return Clean(bars);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private async Task<List<DailyBar>?> TryStooqAsync(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            var client = _http.CreateClient("market");
            var symbol = ticker.ToLowerInvariant() + ".us";
            var url = $"https://stooq.com/q/d/l/?s={Uri.EscapeDataString(symbol)}&i=d";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var csv = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!csv.Contains("Close", StringComparison.OrdinalIgnoreCase))
                return null;

            var bars = new List<DailyBar>();
            foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1))
            {
                var parts = line.Split(',');
                if (parts.Length < 5)
                    continue;
                if (!DateOnly.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    continue;
                if (!double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var close))
                    continue;
                bars.Add(new DailyBar(date, close));
            }

            return Clean(bars);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    private async Task SaveAsync(string ticker, List<DailyBar> bars, string origin, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(bars, JsonOptions);
        var row = await _db.Series.FindAsync([ticker], cancellationToken);
        if (row is null)
        {
            _db.Series.Add(new CachedSeries
            {
                Ticker = ticker,
                BarsJson = json,
                Origin = origin,
                StoredAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            row.BarsJson = json;
            row.Origin = origin;
            row.StoredAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<(string Origin, List<DailyBar> Bars)?> ReadCacheAsync(string ticker, CancellationToken cancellationToken)
    {
        var row = await _db.Series.AsNoTracking().FirstOrDefaultAsync(item => item.Ticker == ticker, cancellationToken);
        if (row is null || string.IsNullOrWhiteSpace(row.BarsJson))
            return null;
        var bars = JsonSerializer.Deserialize<List<DailyBar>>(row.BarsJson, JsonOptions);
        var clean = bars is null ? null : Clean(bars);
        if (clean is null)
            return null;
        var origin = row.Origin == "bundled sample" ? "bundled sample" : "saved cache";
        return (origin, clean);
    }

    private List<DailyBar>? ReadSample(string ticker)
    {
        var path = Path.Combine(_samplePath, ticker + ".json");
        if (!File.Exists(path))
            return null;
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

        return Clean(bars);
    }

    private static List<DailyBar>? Clean(List<DailyBar> bars)
    {
        var byDate = new Dictionary<DateOnly, DailyBar>();
        foreach (var bar in bars)
        {
            if (bar.Close <= 0 || double.IsNaN(bar.Close) || double.IsInfinity(bar.Close))
                continue;
            byDate[bar.Date] = bar;
        }

        var ordered = byDate.Values.OrderBy(bar => bar.Date).ToList();
        return ordered.Count >= 30 ? ordered : null;
    }
}

public sealed class CachedSeries
{
    public string Ticker { get; set; } = "";
    public string BarsJson { get; set; } = "";
    public string Origin { get; set; } = "";
    public DateTime StoredAtUtc { get; set; }
}

public sealed class AskedQuestion
{
    public long Id { get; set; }
    public string Question { get; set; } = "";
    public string Ticker { get; set; } = "";
    public double Probability { get; set; }
    public string PayloadJson { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CacheDb : DbContext
{
    public CacheDb(DbContextOptions<CacheDb> options) : base(options)
    {
    }

    public DbSet<CachedSeries> Series => Set<CachedSeries>();
    public DbSet<AskedQuestion> Questions => Set<AskedQuestion>();
    public DbSet<JevAsk.Api.Fun.FunCard> FunCards => Set<JevAsk.Api.Fun.FunCard>();
    public DbSet<JevAsk.Api.Fun.FunAskRow> FunAsks => Set<JevAsk.Api.Fun.FunAskRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCharSet("utf8mb4");
        modelBuilder.Entity<CachedSeries>(entity =>
        {
            entity.HasKey(row => row.Ticker);
            entity.Property(row => row.Ticker).HasMaxLength(16);
            entity.Property(row => row.Origin).HasMaxLength(64);
            entity.Property(row => row.BarsJson).HasColumnType("longtext");
        });

        modelBuilder.Entity<AskedQuestion>(entity =>
        {
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Question).HasMaxLength(500);
            entity.Property(row => row.Ticker).HasMaxLength(16);
            entity.Property(row => row.PayloadJson).HasColumnType("longtext");
            entity.HasIndex(row => row.CreatedAtUtc);
        });

        modelBuilder.Entity<JevAsk.Api.Fun.FunCard>(entity =>
        {
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Question).HasMaxLength(500);
            entity.Property(row => row.Reasoning).HasColumnType("longtext");
            entity.Property(row => row.Category).HasMaxLength(64);
            entity.Property(row => row.Tag).HasMaxLength(32);
            entity.HasIndex(row => row.Category);
        });

        modelBuilder.Entity<JevAsk.Api.Fun.FunAskRow>(entity =>
        {
            entity.ToTable("FunAsks");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Question).HasMaxLength(500);
            entity.Property(row => row.Reasoning).HasColumnType("longtext");
            entity.Property(row => row.Category).HasMaxLength(64);
            entity.Property(row => row.Tag).HasMaxLength(32);
            entity.Property(row => row.Source).HasMaxLength(16);
            entity.HasIndex(row => row.CreatedAtUtc);
        });
    }
}

public static class SqliteConnection
{
    public static string Normalize(string? connectionString, string contentRoot)
    {
        var cs = string.IsNullOrWhiteSpace(connectionString)
            ? "Data Source=data/jev-ask.db"
            : connectionString.Trim();
        const string prefix = "Data Source=";
        var semi = cs.IndexOf(';');
        var first = semi >= 0 ? cs[..semi] : cs;
        var rest = semi >= 0 ? cs[semi..] : "";
        if (!first.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return cs;

        var source = first[prefix.Length..].Trim();
        if (!Path.IsPathRooted(source))
            source = Path.Combine(contentRoot, source);
        var dir = Path.GetDirectoryName(source);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        return prefix + source + rest;
    }
}
