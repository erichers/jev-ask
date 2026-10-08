using System.Text;

namespace JevAsk.Core.Fun;

public static class FunText
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var sb = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var ch in text.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (pendingSpace && sb.Length > 0)
                    sb.Append(' ');
                pendingSpace = false;
                sb.Append(ch);
            }
            else
            {
                pendingSpace = true;
            }
        }

        return sb.ToString();
    }

    public static string[] Tokens(string normalized) =>
        normalized.Length == 0
            ? []
            : normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
