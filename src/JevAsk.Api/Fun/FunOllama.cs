using System.Text;
using System.Text.Json;

namespace JevAsk.Api.Fun;

public static class FunOllama
{
    public static async Task<string?> TryReasonAsync(HttpClient http, string question, int percent, CancellationToken cancellationToken)
    {
        var url = Environment.GetEnvironmentVariable("OLLAMA_URL");
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL");
        if (string.IsNullOrWhiteSpace(model))
            model = "llama3.1";

        var prompt =
            "Write 2 or 3 short playful sentences for a fun likelihood. " +
            "The likelihood is already decided at " + percent + " percent. Do not change that number. " +
            "Do not invent news. No politics. Be kind. No emoji. " +
            "Question: " + question;
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                model,
                stream = false,
                messages = new[] { new { role = "user", content = prompt } }
            });
            using var request = new HttpRequestMessage(HttpMethod.Post, url.Trim().TrimEnd('/') + "/api/chat")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement.GetProperty("message").GetProperty("content").GetString();
            return Clean(text);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return null;
        }
    }

    public static string? Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var flat = string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (flat.Length is < 40 or > 480)
            return null;
        var sentences = 0;
        foreach (var ch in flat)
        {
            if (ch is '.' or '!' or '?')
                sentences++;
        }

        return sentences >= 2 ? flat : null;
    }
}
