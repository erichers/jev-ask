using System.Globalization;
using System.Text.RegularExpressions;

namespace JevAsk.Core.Parsing;

/// <summary>
/// No-key parser for plain-English market questions.
/// Dates are resolved against the supplied as-of day.
/// </summary>
public sealed partial class RuleBasedParser : IQuestionParser
{
    public const string ParserName = "rule-based";

    public string Name => ParserName;

    public Task<ParseOutcome> ParseAsync(string question, DateOnly asOf, CancellationToken cancellationToken) =>
        Task.FromResult(Parse(question, asOf));

    public ParseOutcome Parse(string question, DateOnly asOf)
    {
        if (string.IsNullOrWhiteSpace(question))
            return ParseOutcome.Fail("Type a market question first.", ParserName);

        var raw = question.Trim();
        if (raw.Length > 500)
            return ParseOutcome.Fail("That question is too long. Keep it under 500 characters.", ParserName);

        var text = raw.ToLowerInvariant();

        if (!TryTicker(raw, text, out var ticker))
            return ParseOutcome.Fail(
                "No ticker found. Use a symbol such as NVDA or a company name such as Nvidia.",
                ParserName);

        var ignored = new List<(int Start, int Length)>();
        foreach (var (phrase, _) in Aliases)
        {
            var at = IndexOfPhrase(text, phrase);
            if (at >= 0)
                ignored.Add((at, phrase.Length));
        }

        if (!TryExpiry(text, asOf, out var expiry, out var assumed, out var dateSpans))
            return ParseOutcome.Fail("Could not read that date.", ParserName);

        ignored.AddRange(dateSpans);

        if (!TryLevel(text, ignored, out var mode, out var level))
            return ParseOutcome.Fail(
                "No price or percent found. Try a level such as 150 or 5%.",
                ParserName);

        var direction = FindDirection(text);
        var style = FindStyle(text) ?? BarrierStyle.Close;

        var intent = new ParsedIntent(ticker, direction, style, mode, level, expiry, assumed, raw);
        return ParseOutcome.Ok(intent, ParserName);
    }

    private static bool TryTicker(string raw, string lower, out string ticker)
    {
        var dollar = DollarTicker().Match(raw);
        if (dollar.Success)
        {
            ticker = dollar.Groups[1].Value.ToUpperInvariant();
            return true;
        }

        if (TryAlias(lower, out ticker))
            return true;

        foreach (Match match in UpperTicker().Matches(raw))
        {
            var symbol = match.Value.ToUpperInvariant();
            if (Stopwords.Contains(symbol))
                continue;
            ticker = symbol;
            return true;
        }

        ticker = "";
        return false;
    }

    private static bool TryAlias(string lower, out string ticker)
    {
        ticker = "";
        var bestAt = int.MaxValue;
        var bestLen = -1;
        foreach (var (phrase, symbol) in Aliases)
        {
            var at = IndexOfPhrase(lower, phrase);
            if (at < 0)
                continue;
            if (at < bestAt || (at == bestAt && phrase.Length > bestLen))
            {
                bestAt = at;
                bestLen = phrase.Length;
                ticker = symbol;
            }
        }

        return bestAt < int.MaxValue;
    }

    private static int IndexOfPhrase(string text, string phrase)
    {
        var from = 0;
        while (from < text.Length)
        {
            var at = text.IndexOf(phrase, from, StringComparison.Ordinal);
            if (at < 0)
                return -1;
            if (IsBoundary(text, at, phrase.Length))
                return at;
            from = at + 1;
        }

        return -1;
    }

    private static bool IsBoundary(string text, int at, int length)
    {
        var left = at == 0 || !char.IsLetterOrDigit(text[at - 1]);
        var end = at + length;
        var right = end >= text.Length || !char.IsLetterOrDigit(text[end]);
        return left && right;
    }

    private static bool TryExpiry(
        string text,
        DateOnly asOf,
        out DateOnly expiry,
        out bool assumed,
        out List<(int Start, int Length)> spans)
    {
        var hits = new List<(int Index, int Length, DateOnly Date)>();
        void Add(Match match, DateOnly date)
        {
            if (match.Success)
                hits.Add((match.Index, match.Length, date));
        }

        foreach (Match match in IsoDate().Matches(text))
        {
            if (TryParts(match, 1, 2, 3, out var date))
                Add(match, date);
        }

        foreach (Match match in UsDate().Matches(text))
        {
            if (int.TryParse(match.Groups[1].Value, out var month)
                && int.TryParse(match.Groups[2].Value, out var day)
                && int.TryParse(match.Groups[3].Value, out var year)
                && month is >= 1 and <= 12
                && day is >= 1 and <= 31)
            {
                Add(match, Safe(year, month, day));
            }
        }

        foreach (Match match in MonthDay().Matches(text))
        {
            var month = MonthIndex(match.Groups[1].Value);
            if (month == 0 || !int.TryParse(match.Groups[2].Value, out var day) || day is < 1 or > 31)
                continue;
            DateOnly date;
            if (match.Groups[3].Success && int.TryParse(match.Groups[3].Value, out var year))
                date = Safe(year, month, day);
            else
                date = ThisOrNextYear(asOf, month, day);
            Add(match, date);
        }

        foreach (Match match in Weekday().Matches(text))
        {
            var qualifier = match.Groups[1].Value;
            var dow = Dow(match.Groups[2].Value);
            var date = qualifier == "next" ? NextWeekday(asOf, dow) : Upcoming(asOf, dow);
            Add(match, date);
        }

        foreach (Match match in EndOfMonth().Matches(text))
            Add(match, EndOfMonthDate(asOf));

        foreach (Match match in EndOfYear().Matches(text))
            Add(match, new DateOnly(asOf.Year, 12, 31));

        foreach (Match match in ThisMonth().Matches(text))
            Add(match, EndOfMonthDate(asOf));

        foreach (Match match in NextWeek().Matches(text))
            Add(match, asOf.AddDays(7));

        foreach (Match match in ThisWeek().Matches(text))
            Add(match, ThisWeekFriday(asOf));

        foreach (Match match in InDays().Matches(text))
        {
            if (int.TryParse(match.Groups[1].Value, out var days) && days is > 0 and < 5000)
                Add(match, asOf.AddDays(days));
        }

        foreach (Match match in Days().Matches(text))
        {
            if (int.TryParse(match.Groups[1].Value, out var days) && days is > 0 and < 5000)
                Add(match, asOf.AddDays(days));
        }

        spans = hits.Select(hit => (hit.Index, hit.Length)).ToList();
        if (hits.Count == 0)
        {
            expiry = asOf.AddDays(30);
            assumed = true;
            return true;
        }

        var best = hits
            .OrderBy(hit => hit.Index)
            .ThenByDescending(hit => hit.Length)
            .First();
        expiry = best.Date;
        assumed = false;
        spans = [(best.Index, best.Length)];
        return true;
    }

    private static bool TryLevel(
        string text,
        List<(int Start, int Length)> ignored,
        out LevelMode mode,
        out double level)
    {
        foreach (Match match in Percent().Matches(text))
        {
            if (Overlaps(match.Index, match.Length, ignored))
                continue;
            if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out level)
                && level > 0)
            {
                mode = LevelMode.Percent;
                return true;
            }
        }

        foreach (Match match in Dollar().Matches(text))
        {
            if (Overlaps(match.Index, match.Length, ignored))
                continue;
            if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out level)
                && level > 0)
            {
                mode = LevelMode.Absolute;
                return true;
            }
        }

        foreach (Match match in BareNumber().Matches(text))
        {
            if (Overlaps(match.Index, match.Length, ignored))
                continue;
            if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out level)
                && level > 0)
            {
                mode = LevelMode.Absolute;
                return true;
            }
        }

        mode = LevelMode.Absolute;
        level = 0;
        return false;
    }

    private static BarrierDirection? FindDirection(string text)
    {
        int best = int.MaxValue;
        BarrierDirection? found = null;
        foreach (var (pattern, direction) in Directions)
        {
            var match = Regex.Match(text, pattern, RegexOptions.CultureInvariant);
            if (match.Success && match.Index < best)
            {
                best = match.Index;
                found = direction;
            }
        }

        return found;
    }

    private static BarrierStyle? FindStyle(string text)
    {
        int best = int.MaxValue;
        BarrierStyle? found = null;
        foreach (var (pattern, style) in Styles)
        {
            var match = Regex.Match(text, pattern, RegexOptions.CultureInvariant);
            if (match.Success && match.Index < best)
            {
                best = match.Index;
                found = style;
            }
        }

        return found;
    }

    private static bool Overlaps(int start, int length, List<(int Start, int Length)> spans)
    {
        var end = start + length;
        foreach (var (spanStart, spanLength) in spans)
        {
            var spanEnd = spanStart + spanLength;
            if (start < spanEnd && spanStart < end)
                return true;
        }

        return false;
    }

    private static bool TryParts(Match match, int yearGroup, int monthGroup, int dayGroup, out DateOnly date)
    {
        date = default;
        if (!int.TryParse(match.Groups[yearGroup].Value, out var year)
            || !int.TryParse(match.Groups[monthGroup].Value, out var month)
            || !int.TryParse(match.Groups[dayGroup].Value, out var day))
            return false;
        if (month is < 1 or > 12 || day is < 1 or > 31 || year is < 1990 or > 2200)
            return false;
        date = Safe(year, month, day);
        return true;
    }

    private static DateOnly Safe(int year, int month, int day)
    {
        var dim = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, dim));
    }

    private static DateOnly ThisOrNextYear(DateOnly asOf, int month, int day)
    {
        var date = Safe(asOf.Year, month, day);
        if (date < asOf)
            date = Safe(asOf.Year + 1, month, day);
        return date;
    }

    private static DateOnly EndOfMonthDate(DateOnly asOf)
    {
        var dim = DateTime.DaysInMonth(asOf.Year, asOf.Month);
        return new DateOnly(asOf.Year, asOf.Month, dim);
    }

    private static DateOnly Upcoming(DateOnly asOf, DayOfWeek dow)
    {
        var delta = ((int)dow - (int)asOf.DayOfWeek + 7) % 7;
        return asOf.AddDays(delta);
    }

    private static DateOnly NextWeekday(DateOnly asOf, DayOfWeek dow)
    {
        var diffToMonday = ((int)asOf.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var nextWeekStart = asOf.AddDays(-diffToMonday + 7);
        var index = ((int)dow - (int)DayOfWeek.Monday + 7) % 7;
        return nextWeekStart.AddDays(index);
    }

    private static DateOnly ThisWeekFriday(DateOnly asOf)
    {
        var friday = Upcoming(asOf, DayOfWeek.Friday);
        var diffToMonday = ((int)asOf.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var weekStart = asOf.AddDays(-diffToMonday);
        var weekEnd = weekStart.AddDays(6);
        if (friday < asOf || friday > weekEnd)
            return NextWeekday(asOf, DayOfWeek.Friday);
        return friday;
    }

    private static DayOfWeek Dow(string name) => name switch
    {
        "monday" => DayOfWeek.Monday,
        "tuesday" => DayOfWeek.Tuesday,
        "wednesday" => DayOfWeek.Wednesday,
        "thursday" => DayOfWeek.Thursday,
        "friday" => DayOfWeek.Friday,
        "saturday" => DayOfWeek.Saturday,
        "sunday" => DayOfWeek.Sunday,
        _ => DayOfWeek.Friday
    };

    private static int MonthIndex(string token)
    {
        var key = token.Length >= 3 ? token[..3] : token;
        return key switch
        {
            "jan" => 1,
            "feb" => 2,
            "mar" => 3,
            "apr" => 4,
            "may" => 5,
            "jun" => 6,
            "jul" => 7,
            "aug" => 8,
            "sep" => 9,
            "oct" => 10,
            "nov" => 11,
            "dec" => 12,
            _ => 0
        };
    }

    private static readonly (string Pattern, BarrierDirection Direction)[] Directions =
    [
        (@"\b(lower than|less than|at most|below|under|drops|dropping|drop|falls|falling|fall|declines|decline|loses|lose|down)\b", BarrierDirection.Below),
        (@"\b(higher than|greater than|at least|exceeds|exceed|above|over|rises|rising|rise|gains|gaining|gain|rallies|rally|climbs|climbing|climb|up)\b", BarrierDirection.Above)
    ];

    private static readonly (string Pattern, BarrierStyle Style)[] Styles =
    [
        (@"\b(touches|touching|touch|hits|hitting|hit|reaches|reaching|reach)\b", BarrierStyle.Touch),
        (@"\b(closes|closing|close|finishes|finishing|finish|settles|settling|settle)\b", BarrierStyle.Close)
    ];

    private static readonly (string Phrase, string Ticker)[] Aliases =
    [
        ("invesco qqq", "QQQ"),
        ("nasdaq-100", "QQQ"),
        ("nasdaq 100", "QQQ"),
        ("s&p 500", "SPY"),
        ("s&p500", "SPY"),
        ("sp 500", "SPY"),
        ("sp500", "SPY"),
        ("s&p", "SPY"),
        ("advanced micro devices", "AMD"),
        ("meta platforms", "META"),
        ("nvidia", "NVDA"),
        ("microsoft", "MSFT"),
        ("alphabet", "GOOGL"),
        ("facebook", "META"),
        ("amazon", "AMZN"),
        ("netflix", "NFLX"),
        ("google", "GOOGL"),
        ("apple", "AAPL"),
        ("tesla", "TSLA"),
        ("intel", "INTC"),
        ("jp morgan", "JPM"),
        ("jpmorgan", "JPM"),
        ("broadcom", "AVGO"),
        ("russell 2000", "IWM"),
        ("disney", "DIS"),
        ("aapl", "AAPL"),
        ("nvda", "NVDA"),
        ("tsla", "TSLA"),
        ("msft", "MSFT"),
        ("amzn", "AMZN"),
        ("googl", "GOOGL"),
        ("meta", "META"),
        ("nflx", "NFLX"),
        ("intc", "INTC"),
        ("qqq", "QQQ"),
        ("spy", "SPY"),
        ("amd", "AMD")
    ];

    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "WILL", "THE", "BY", "CLOSE", "CLOSES", "ABOVE", "BELOW", "TOUCH", "TOUCHES",
        "BEFORE", "AFTER", "THIS", "NEXT", "MONTH", "WEEK", "FRIDAY", "MONDAY",
        "TUESDAY", "WEDNESDAY", "THURSDAY", "SATURDAY", "SUNDAY", "AND", "OR", "OF",
        "IN", "ON", "FOR", "END", "YEAR", "DAYS", "DAY", "DROP", "DROPS", "RISES",
        "RISE", "GAIN", "GAINS", "FALL", "FALLS", "PERCENT", "OVER", "UNDER", "HIT",
        "HITS", "REACH", "AT", "TO", "FROM", "WITH", "WHAT", "HOW", "IS", "IT", "BE",
        "IF", "CAN", "DOES", "DO", "DID", "CHANCE", "FINISH", "JAN", "FEB", "MAR",
        "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "SEPT", "OCT", "NOV", "DEC", "NOT",
        "THAN", "LOWER", "HIGHER", "AN", "INTO", "WITHIN", "BEFORE"
    };

    [GeneratedRegex(@"\$\s*([A-Za-z]{1,5})\b")]
    private static partial Regex DollarTicker();

    [GeneratedRegex(@"\b([A-Z]{2,5})\b")]
    private static partial Regex UpperTicker();

    [GeneratedRegex(@"\b(\d{4})-(\d{2})-(\d{2})\b")]
    private static partial Regex IsoDate();

    [GeneratedRegex(@"\b(\d{1,2})/(\d{1,2})/(\d{4})\b")]
    private static partial Regex UsDate();

    [GeneratedRegex(@"\b(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\s+(\d{1,2})(?:st|nd|rd|th)?(?:,?\s*(\d{4}))?\b")]
    private static partial Regex MonthDay();

    [GeneratedRegex(@"\b(?:(next|this)\s+)?(monday|tuesday|wednesday|thursday|friday|saturday|sunday)\b")]
    private static partial Regex Weekday();

    [GeneratedRegex(@"\b(?:end of (?:the )?month|month end|eom)\b")]
    private static partial Regex EndOfMonth();

    [GeneratedRegex(@"\b(?:end of (?:the )?year|year end|eoy)\b")]
    private static partial Regex EndOfYear();

    [GeneratedRegex(@"\bthis month\b")]
    private static partial Regex ThisMonth();

    [GeneratedRegex(@"\bnext week\b")]
    private static partial Regex NextWeek();

    [GeneratedRegex(@"\bthis week\b")]
    private static partial Regex ThisWeek();

    [GeneratedRegex(@"\b(?:in|within|after)\s+(\d+)\s+days?\b")]
    private static partial Regex InDays();

    [GeneratedRegex(@"\b(\d+)\s+days?\b")]
    private static partial Regex Days();

    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*(?:%|percent\b)")]
    private static partial Regex Percent();

    [GeneratedRegex(@"\$\s*(\d+(?:\.\d+)?)")]
    private static partial Regex Dollar();

    [GeneratedRegex(@"(?<![\d.])(\d+(?:\.\d+)?)(?![\d.])")]
    private static partial Regex BareNumber();
}
