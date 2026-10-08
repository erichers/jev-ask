using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JevAsk.Core.Parsing;

namespace JevAsk.Api.Parsing;

public sealed record LlmSettings(string Provider, string Name, string Url, string Model, string ApiKey)
{
    public bool Configured =>
        Provider switch
        {
            "ollama" => !string.IsNullOrWhiteSpace(Url),
            "openrouter" or "groq" or "gemini" => !string.IsNullOrWhiteSpace(ApiKey),
            _ => false
        };

    public static LlmSettings Resolve(IConfiguration config)
    {
        string Read(string envName, string configKey)
        {
            var fromEnv = Environment.GetEnvironmentVariable(envName);
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv.Trim();
            return config[configKey]?.Trim() ?? "";
        }

        var provider = Read("PARSER_PROVIDER", "Parser:Provider").ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(provider))
            provider = "rule";

        return provider switch
        {
            "ollama" => new LlmSettings(
                "ollama",
                "ollama:" + Fallback(Read("OLLAMA_MODEL", "Ollama:Model"), "llama3.1"),
                Fallback(Read("OLLAMA_URL", "Ollama:Url"), "http://localhost:11434").TrimEnd('/'),
                Fallback(Read("OLLAMA_MODEL", "Ollama:Model"), "llama3.1"),
                ""),
            "openrouter" => new LlmSettings(
                "openrouter",
                "openrouter:" + Fallback(Read("OPENROUTER_MODEL", "OpenRouter:Model"), "meta-llama/llama-3.1-8b-instruct:free"),
                "https://openrouter.ai/api/v1/chat/completions",
                Fallback(Read("OPENROUTER_MODEL", "OpenRouter:Model"), "meta-llama/llama-3.1-8b-instruct:free"),
                Read("OPENROUTER_API_KEY", "OpenRouter:ApiKey")),
            "groq" => new LlmSettings(
                "groq",
                "groq:" + Fallback(Read("GROQ_MODEL", "Groq:Model"), "llama-3.1-8b-instant"),
                "https://api.groq.com/openai/v1/chat/completions",
                Fallback(Read("GROQ_MODEL", "Groq:Model"), "llama-3.1-8b-instant"),
                Read("GROQ_API_KEY", "Groq:ApiKey")),
            "gemini" => new LlmSettings(
                "gemini",
                "gemini:" + Fallback(Read("GEMINI_MODEL", "Gemini:Model"), "gemini-2.0-flash"),
                "https://generativelanguage.googleapis.com/v1beta/models/",
                Fallback(Read("GEMINI_MODEL", "Gemini:Model"), "gemini-2.0-flash"),
                Read("GEMINI_API_KEY", "Gemini:ApiKey")),
            _ => new LlmSettings("rule", RuleBasedParser.ParserName, "", "", "")
        };
    }

    private static string Fallback(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}

public sealed class SelectingParser : IQuestionParser
{
    private readonly LlmQuestionParser? _llm;
    private readonly RuleBasedParser _rules;
    private readonly LlmSettings _settings;

    public SelectingParser(IHttpClientFactory http, IConfiguration config, RuleBasedParser rules)
        : this(http.CreateClient("llm"), LlmSettings.Resolve(config), rules)
    {
    }

    public SelectingParser(HttpClient http, LlmSettings settings, RuleBasedParser rules)
    {
        _rules = rules;
        _settings = settings;
        _llm = settings.Provider is "ollama" or "openrouter" or "groq" or "gemini"
            ? new LlmQuestionParser(http, settings, rules)
            : null;
    }

    public string Name => _llm?.Name ?? _rules.Name;

    public Task<ParseOutcome> ParseAsync(string question, DateOnly asOf, CancellationToken cancellationToken)
    {
        if (_llm is null)
            return _rules.ParseAsync(question, asOf, cancellationToken);
        return _llm.ParseAsync(question, asOf, cancellationToken);
    }
}

public sealed class LlmQuestionParser : IQuestionParser
{
    private readonly HttpClient _http;
    private readonly LlmSettings _settings;
    private readonly RuleBasedParser _rules;

    public LlmQuestionParser(HttpClient http, LlmSettings settings, RuleBasedParser rules)
    {
        _http = http;
        _settings = settings;
        _rules = rules;
    }

    public string Name => _settings.Name;

    public async Task<ParseOutcome> ParseAsync(string question, DateOnly asOf, CancellationToken cancellationToken)
    {
        if (!_settings.Configured)
            return FellBack(_rules.Parse(question, asOf));

        try
        {
            var content = await CompleteAsync(question, asOf, cancellationToken);
            if (IntentJson.TryParse(content, question, out var intent, out _) && intent is not null)
                return ParseOutcome.Ok(intent, _settings.Name);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
        }

        return FellBack(_rules.Parse(question, asOf));
    }

    private ParseOutcome FellBack(ParseOutcome rule) =>
        rule with { FellBack = true, AttemptedParser = _settings.Name, ParserName = RuleBasedParser.ParserName };

    private async Task<string> CompleteAsync(string question, DateOnly asOf, CancellationToken cancellationToken)
    {
        var prompt = BuildPrompt(question, asOf);
        using var request = BuildRequest(prompt);
        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Model request failed.");
        return ReadContent(_settings.Provider, body);
    }

    public static string BuildPrompt(string question, DateOnly asOf) =>
        "Today is " + asOf.ToString("yyyy-MM-dd") + ". " +
        "Parse the market question into one JSON object and nothing else. " +
        "Keys: ticker (uppercase symbol), condition (above, below, or auto), style (close or touch), " +
        "levelMode (absolute or percent), level (positive number), expiry (YYYY-MM-DD). " +
        "Resolve relative dates against today. Question: " + question;

    private HttpRequestMessage BuildRequest(string prompt)
    {
        if (_settings.Provider == "ollama")
        {
            var payload = JsonSerializer.Serialize(new
            {
                model = _settings.Model,
                stream = false,
                format = "json",
                messages = new[] { new { role = "user", content = prompt } }
            });
            return JsonPost(_settings.Url + "/api/chat", payload, null);
        }

        if (_settings.Provider == "gemini")
        {
            var payload = JsonSerializer.Serialize(new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { responseMimeType = "application/json" }
            });
            var url = _settings.Url + Uri.EscapeDataString(_settings.Model) + ":generateContent?key=" + Uri.EscapeDataString(_settings.ApiKey);
            return JsonPost(url, payload, null);
        }

        var chat = JsonSerializer.Serialize(new
        {
            model = _settings.Model,
            messages = new[] { new { role = "user", content = prompt } },
            response_format = new { type = "json_object" }
        });
        return JsonPost(_settings.Url, chat, _settings.ApiKey);
    }

    private static HttpRequestMessage JsonPost(string url, string json, string? bearer)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrWhiteSpace(bearer))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return request;
    }

    public static string ReadContent(string provider, string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (provider == "ollama")
            return root.GetProperty("message").GetProperty("content").GetString() ?? "";
        if (provider == "gemini")
            return root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        return root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}
