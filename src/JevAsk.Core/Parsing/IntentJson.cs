using System.Globalization;
using System.Text.Json;

namespace JevAsk.Core.Parsing;

public static class IntentJson
{
    public static bool TryParse(string? content, string rawQuestion, out ParsedIntent? intent, out string error)
    {
        intent = null;
        error = "Model output was not valid JSON.";
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var json = ExtractObject(content);
        if (json is null)
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "Model output was not a JSON object.";
                return false;
            }

            if (!TryString(root, "ticker", out var ticker))
            {
                error = "JSON is missing ticker.";
                return false;
            }

            ticker = ticker.Trim().ToUpperInvariant();
            if (ticker.Length is < 1 or > 6 || ticker.Any(ch => !char.IsLetterOrDigit(ch) && ch is not '.' and not '-'))
            {
                error = "JSON ticker is not a symbol.";
                return false;
            }

            if (!TryString(root, "condition", out var conditionText))
            {
                error = "JSON is missing condition.";
                return false;
            }

            BarrierDirection? direction;
            switch (conditionText.Trim().ToLowerInvariant())
            {
                case "above":
                    direction = BarrierDirection.Above;
                    break;
                case "below":
                    direction = BarrierDirection.Below;
                    break;
                case "auto":
                    direction = null;
                    break;
                default:
                    error = "JSON condition must be above, below, or auto.";
                    return false;
            }

            if (!TryString(root, "style", out var styleText))
            {
                error = "JSON is missing style.";
                return false;
            }

            var style = styleText.Trim().ToLowerInvariant() switch
            {
                "close" => BarrierStyle.Close,
                "touch" => BarrierStyle.Touch,
                _ => (BarrierStyle?)null
            };
            if (style is null)
            {
                error = "JSON style must be close or touch.";
                return false;
            }

            if (!TryString(root, "levelMode", out var modeText) && !TryString(root, "levelmode", out modeText))
            {
                error = "JSON is missing levelMode.";
                return false;
            }

            var mode = modeText.Trim().ToLowerInvariant() switch
            {
                "absolute" => LevelMode.Absolute,
                "percent" => LevelMode.Percent,
                _ => (LevelMode?)null
            };
            if (mode is null)
            {
                error = "JSON levelMode must be absolute or percent.";
                return false;
            }

            if (!root.TryGetProperty("level", out var levelEl) || levelEl.ValueKind != JsonValueKind.Number
                || !levelEl.TryGetDouble(out var level) || level <= 0 || double.IsNaN(level) || double.IsInfinity(level))
            {
                error = "JSON level must be a positive number.";
                return false;
            }

            if (!TryString(root, "expiry", out var expiryText)
                || !DateOnly.TryParse(expiryText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
            {
                error = "JSON expiry must be YYYY-MM-DD.";
                return false;
            }

            intent = new ParsedIntent(ticker, direction, style.Value, mode.Value, level, expiry, false, rawQuestion);
            error = "";
            return true;
        }
        catch (JsonException)
        {
            error = "Model output was not valid JSON.";
            return false;
        }
    }

    public static string? ExtractObject(string content)
    {
        var start = content.IndexOf('{');
        if (start < 0)
            return null;

        var depth = 0;
        var inString = false;
        var escape = false;
        for (var i = start; i < content.Length; i++)
        {
            var ch = content[i];
            if (inString)
            {
                if (escape)
                    escape = false;
                else if (ch == '\\')
                    escape = true;
                else if (ch == '"')
                    inString = false;
                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{')
                depth++;
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                    return content[start..(i + 1)];
            }
        }

        return null;
    }

    private static bool TryString(JsonElement root, string name, out string value)
    {
        value = "";
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            return false;
        value = el.GetString() ?? "";
        return value.Length > 0;
    }
}
