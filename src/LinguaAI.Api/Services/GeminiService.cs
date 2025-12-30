using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LinguaAI.Api.Services;

public interface IGeminiService
{
    Task<string> ChatAsync(string language, string scenario, string message, List<(string role, string content)> history);
    Task<(int score, string feedback, List<string> corrections)> EvaluatePronunciationAsync(string language, string target, string spoken);
    Task<(string corrected, List<(string orig, string fix, string explanation)> corrections, List<string> suggestions)> CheckWritingAsync(string language, string text, string level);
    Task<(string title, string content, List<(string word, string meaning, string pronunciation, string example)> vocabulary, List<(string question, List<string> options, int correctIndex)> quiz)> GenerateReadingAsync(string language, string level, string? topic);
    Task<List<(string word, string meaning, string pronunciation, string example)>> GenerateVocabularyAsync(string language, string theme, int count);
    Task<string> TranslateAsync(string text, string targetLanguage);
}

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GeminiService> _logger;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        { "en", "English" },
        { "ko", "Korean" },
        { "zh", "Chinese (Mandarin)" }
    };

    public GeminiService(IConfiguration config, ILogger<GeminiService> logger)
    {
        _logger = logger;
        _apiKey = config["Gemini:ApiKey"] ?? "";
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Gemini API key not configured. Set environment variable: Gemini__ApiKey");
        }
        
        _httpClient = new HttpClient();
    }

    private async Task<string> CallGeminiAsync(string prompt)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        
        // Retry with exponential backoff for rate limiting
        int maxRetries = 3;
        int delayMs = 2000; // Start with 2 seconds

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}?key={_apiKey}", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseText);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? "";
            }

            // Check if rate limited
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt < maxRetries)
                {
                    _logger.LogWarning("Rate limited, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})", 
                        delayMs, attempt + 1, maxRetries);
                    await Task.Delay(delayMs);
                    delayMs *= 2; // Exponential backoff
                    continue;
                }
            }

            _logger.LogError("Gemini API error: {Response}", responseText);
            throw new Exception($"Gemini API error: {response.StatusCode}. Vui lòng thử lại sau ít phút.");
        }

        throw new Exception("Không thể kết nối Gemini API sau nhiều lần thử.");
    }

    public async Task<string> ChatAsync(string language, string scenario, string message, List<(string role, string content)> history)
    {
        var langName = GetLanguageName(language);
        var systemPrompt = $@"You are a friendly native {langName} speaker helping someone practice {langName} conversation.
Scenario: {scenario}
Rules:
- Respond ONLY in {langName}
- Keep responses conversational and natural
- If the user makes mistakes, gently correct them
- Adapt to the user's level
- After your response, add a line break and provide a brief translation in Vietnamese";

        var prompt = BuildConversationPrompt(systemPrompt, history, message);
        return await CallGeminiAsync(prompt);
    }

    public async Task<(int score, string feedback, List<string> corrections)> EvaluatePronunciationAsync(string language, string target, string spoken)
    {
        var langName = GetLanguageName(language);
        var prompt = $@"You are a {langName} pronunciation expert. Compare the target text with what was spoken.

Target: {target}
Spoken: {spoken}

Respond in JSON format only (no markdown):
{{
    ""score"": <0-100>,
    ""feedback"": ""<brief feedback in Vietnamese>"",
    ""corrections"": [""<specific pronunciation tips in Vietnamese>""]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            var json = ExtractJson(response);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return (
                root.GetProperty("score").GetInt32(),
                root.GetProperty("feedback").GetString() ?? "",
                root.GetProperty("corrections").EnumerateArray().Select(x => x.GetString() ?? "").ToList()
            );
        }
        catch
        {
            return (50, "Không thể đánh giá phát âm", new List<string>());
        }
    }

    public async Task<(string corrected, List<(string orig, string fix, string explanation)> corrections, List<string> suggestions)> CheckWritingAsync(string language, string text, string level)
    {
        var langName = GetLanguageName(language);
        var prompt = $@"You are a {langName} writing tutor. Check the following text for grammar, spelling, and style.
Level: {level}
Text: {text}

Respond in JSON format only (no markdown):
{{
    ""correctedText"": ""<corrected version>"",
    ""corrections"": [
        {{""original"": ""<wrong part>"", ""corrected"": ""<fixed part>"", ""explanation"": ""<explanation in Vietnamese>""}}
    ],
    ""suggestions"": [""<improvement suggestions in Vietnamese>""]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            var json = ExtractJson(response);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return (
                root.GetProperty("correctedText").GetString() ?? text,
                root.GetProperty("corrections").EnumerateArray().Select(x => (
                    x.GetProperty("original").GetString() ?? "",
                    x.GetProperty("corrected").GetString() ?? "",
                    x.GetProperty("explanation").GetString() ?? ""
                )).ToList(),
                root.GetProperty("suggestions").EnumerateArray().Select(x => x.GetString() ?? "").ToList()
            );
        }
        catch
        {
            return (text, new List<(string, string, string)>(), new List<string>());
        }
    }

    public async Task<(string title, string content, List<(string word, string meaning, string pronunciation, string example)> vocabulary, List<(string question, List<string> options, int correctIndex)> quiz)> GenerateReadingAsync(string language, string level, string? topic)
    {
        var langName = GetLanguageName(language);
        var topicPart = string.IsNullOrEmpty(topic) ? "any interesting topic" : topic;
        var prompt = $@"Generate a short reading passage in {langName} for {level} level learners about {topicPart}.

Respond in JSON format only (no markdown):
{{
    ""title"": ""<title in {langName}>"",
    ""content"": ""<3-5 paragraphs in {langName}>"",
    ""vocabulary"": [
        {{""word"": ""<word>"", ""meaning"": ""<Vietnamese meaning>"", ""pronunciation"": ""<romanization if applicable>"", ""example"": ""<example sentence>""}}
    ],
    ""questions"": [
        {{""question"": ""<comprehension question in Vietnamese>"", ""options"": [""A"", ""B"", ""C"", ""D""], ""correctIndex"": 0}}
    ]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            var json = ExtractJson(response);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return (
                root.GetProperty("title").GetString() ?? "",
                root.GetProperty("content").GetString() ?? "",
                root.GetProperty("vocabulary").EnumerateArray().Select(x => (
                    x.GetProperty("word").GetString() ?? "",
                    x.GetProperty("meaning").GetString() ?? "",
                    x.GetProperty("pronunciation").GetString() ?? "",
                    x.GetProperty("example").GetString() ?? ""
                )).ToList(),
                root.GetProperty("questions").EnumerateArray().Select(x => (
                    x.GetProperty("question").GetString() ?? "",
                    x.GetProperty("options").EnumerateArray().Select(o => o.GetString() ?? "").ToList(),
                    x.GetProperty("correctIndex").GetInt32()
                )).ToList()
            );
        }
        catch
        {
            return ("Error", "Could not generate content", new List<(string, string, string, string)>(), new List<(string, List<string>, int)>());
        }
    }

    public async Task<List<(string word, string meaning, string pronunciation, string example)>> GenerateVocabularyAsync(string language, string theme, int count)
    {
        var langName = GetLanguageName(language);
        var prompt = $@"Generate {count} vocabulary words in {langName} related to the theme: {theme}.

Respond in JSON format only (no markdown):
{{
    ""words"": [
        {{""word"": ""<word in {langName}>"", ""meaning"": ""<Vietnamese meaning>"", ""pronunciation"": ""<romanization>"", ""example"": ""<example sentence>""}}
    ]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            var json = ExtractJson(response);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("words").EnumerateArray().Select(x => (
                x.GetProperty("word").GetString() ?? "",
                x.GetProperty("meaning").GetString() ?? "",
                x.GetProperty("pronunciation").GetString() ?? "",
                x.GetProperty("example").GetString() ?? ""
            )).ToList();
        }
        catch
        {
            return new List<(string, string, string, string)>();
        }
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage)
    {
        var targetLangName = targetLanguage switch
        {
            "en" => "English",
            "zh" => "Chinese (Simplified)",
            "vi" => "Vietnamese",
            "ko" => "Korean",
            _ => "English"
        };

        var prompt = $@"Translate the following text to {targetLangName}. 
Return ONLY the translation, nothing else. No explanations, no quotes.

Text: {text}

Translation:";

        try
        {
            var translation = await CallGeminiAsync(prompt);
            return translation.Trim().Trim('"');
        }
        catch
        {
            return text; // Return original if translation fails
        }
    }

    private string GetLanguageName(string code) => LanguageNames.GetValueOrDefault(code, "English");

    private string BuildConversationPrompt(string system, List<(string role, string content)> history, string currentMessage)
    {
        var sb = new StringBuilder();
        sb.AppendLine(system);
        sb.AppendLine();
        foreach (var (role, content) in history)
        {
            sb.AppendLine($"{role}: {content}");
        }
        sb.AppendLine($"User: {currentMessage}");
        sb.AppendLine("Assistant:");
        return sb.ToString();
    }

    private string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text.Substring(start, end - start + 1);
        }
        return "{}";
    }
}
