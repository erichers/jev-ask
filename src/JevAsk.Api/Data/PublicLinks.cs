namespace JevAsk.Api.Data;

public static class PublicLinks
{
    public static string Normalize(string? configured)
    {
        var value = (configured ?? "").Trim();
        return value.TrimEnd('/');
    }

    public static string? Question(string? publicBaseUrl, long id)
    {
        var root = Normalize(publicBaseUrl);
        if (root.Length == 0 || id <= 0)
            return null;
        return root + "/q/" + id.ToString();
    }

    public static string? Fun(string? publicBaseUrl, long id)
    {
        var root = Normalize(publicBaseUrl);
        if (root.Length == 0 || id <= 0)
            return null;
        return root + "/fun/" + id.ToString();
    }
}
