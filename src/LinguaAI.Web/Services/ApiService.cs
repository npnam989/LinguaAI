using System.Security.Cryptography;
using System.Text;
using LinguaAI.Common.Models;

namespace LinguaAI.Web.Services;

public interface IApiService
{
    Task<VocabularyResponse?> GenerateVocabularyAsync(string language, string theme, int count);
    Task<VocabularyResponse?> UploadVocabularyFileAsync(Stream fileStream, string fileName);
    Task<TranslateResponse?> TranslateAsync(string text, string targetLanguage);
    Task<ReadingResponse?> GenerateReadingAsync(string language, string level, string? topic);
    Task<PronunciationResponse?> EvaluatePronunciationAsync(string language, string target, string spoken);
    Task<WritingResponse?> CheckWritingAsync(string language, string text, string level);
    Task<List<PronunciationPhrase>> GetPhrasesAsync(string language);
    Task<ChatResponse?> ChatAsync(string language, string scenario, string message, List<ChatMessage> history);
    Task<List<ThemeItem>> GetThemesAsync();
    Task<string> TranscribeAudioAsync(byte[] audioData, string language, string mimeType = "audio/webm");
    Task<PracticeResponse?> GeneratePracticeExercisesAsync(PracticeRequest request);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ApiService> _logger;
    
    private const long WINDOW_TICKS = 600_000_000; // 60 seconds in ticks
    private const long TICKS_EPOCH = 621355968000000000; // .NET epoch

    public ApiService(HttpClient httpClient, IConfiguration config, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<VocabularyResponse?> GenerateVocabularyAsync(string language, string theme, int count)
    {
        try
        {
            var request = new VocabularyRequest { Language = language, Theme = theme, Count = count };
            var response = await SendWithAuthAsync<VocabularyResponse>(
                HttpMethod.Post, 
                "/api/vocabulary/generate", 
                request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating vocabulary");
            return null;
        }
    }

    public async Task<VocabularyResponse?> UploadVocabularyFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            // Copy stream to memory to allow re-reading
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(memoryStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                fileName.EndsWith(".xlsx") 
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            content.Add(streamContent, "file", fileName);

            AddAuthHeader();
            var response = await _httpClient.PostAsync("/api/vocabulary/upload", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Upload response: {Status} - {Body}", response.StatusCode, responseBody);
            
            if (response.IsSuccessStatusCode)
            {
                return System.Text.Json.JsonSerializer.Deserialize<VocabularyResponse>(responseBody, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            
            _logger.LogWarning("Upload failed: {Status} - {Body}", response.StatusCode, responseBody);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading vocabulary file");
            return null;
        }
    }

    public async Task<TranslateResponse?> TranslateAsync(string text, string targetLanguage)
    {
        try
        {
            var request = new TranslateRequest { Text = text, TargetLanguage = targetLanguage };
            return await SendWithAuthAsync<TranslateResponse>(
                HttpMethod.Post, 
                "/api/vocabulary/translate", 
                request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating text");
            return null;
        }
    }

    public async Task<ReadingResponse?> GenerateReadingAsync(string language, string level, string? topic)
    {
        try
        {
            var request = new ReadingRequest { Language = language, Level = level, Topic = topic };
            return await SendWithAuthAsync<ReadingResponse>(HttpMethod.Post, "/api/reading/generate", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reading");
            return null;
        }
    }

    public async Task<PronunciationResponse?> EvaluatePronunciationAsync(string language, string target, string spoken)
    {
        try
        {
            var request = new PronunciationRequest { Language = language, TargetText = target, SpokenText = spoken };
            return await SendWithAuthAsync<PronunciationResponse>(HttpMethod.Post, "/api/pronunciation/evaluate", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating pronunciation");
            return null;
        }
    }

    public async Task<WritingResponse?> CheckWritingAsync(string language, string text, string level)
    {
        try
        {
            var request = new WritingRequest { Language = language, Text = text, Level = level };
            return await SendWithAuthAsync<WritingResponse>(HttpMethod.Post, "/api/writing/check", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking writing");
            return null;
        }
    }

    public async Task<ChatResponse?> ChatAsync(string language, string scenario, string message, List<ChatMessage> history)
    {
        try
        {
            var request = new ChatRequest { Language = language, Scenario = scenario, Message = message, History = history };
            return await SendWithAuthAsync<ChatResponse>(HttpMethod.Post, "/api/conversation/chat", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking chat");
            return null;
        }
    }

    public async Task<List<PronunciationPhrase>> GetPhrasesAsync(string language)
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.GetFromJsonAsync<List<PronunciationPhrase>>($"/api/pronunciation/phrases/{language}");
            return response ?? new List<PronunciationPhrase>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting phrases");
            return new List<PronunciationPhrase>();
        }
    }

    public async Task<List<ThemeItem>> GetThemesAsync()
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.GetFromJsonAsync<List<ThemeItem>>("/api/vocabulary/themes");
            return response ?? new List<ThemeItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting themes");
            return new List<ThemeItem>();
        }
    }

    private async Task<T?> SendWithAuthAsync<T>(HttpMethod method, string endpoint, object? body = null)
    {
        AddAuthHeader();
        
        HttpResponseMessage response;
        if (method == HttpMethod.Post && body != null)
        {
            response = await _httpClient.PostAsJsonAsync(endpoint, body);
        }
        else if (method == HttpMethod.Get)
        {
            response = await _httpClient.GetAsync(endpoint);
        }
        else
        {
            throw new NotSupportedException($"Method {method} not supported");
        }

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }

        _logger.LogWarning("API request failed: {Status} for {Endpoint}", response.StatusCode, endpoint);
        return default;
    }

    private void AddAuthHeader()
    {
        var userId = _config["Auth:UserId"]?.Trim();
        var apiKey = _config["Auth:ApiKey"]?.Trim();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Cannot add Auth Header: Missing credentials. UserId: {HasUser}, ApiKey: {HasKey}", 
                !string.IsNullOrEmpty(userId), !string.IsNullOrEmpty(apiKey));
            return;
        }

        var ticks = DateTime.UtcNow.Ticks;
        var window = ticks / WINDOW_TICKS;
        var password = ComputeSha256(FormattableString.Invariant($"{apiKey}{window}"));
        var hash = ComputeSha256(FormattableString.Invariant($"{userId}:{password}"));

        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"HMAC-SHA256 {userId}:{hash}");
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<string> TranscribeAudioAsync(byte[] audioData, string language, string mimeType = "audio/webm")
    {
        try
        {
            var payload = new { 
                AudioData = audioData, 
                Language = language,
                MimeType = mimeType
            };
            
            var response = await SendWithAuthAsync<TranscribeResponse>(HttpMethod.Post, "/api/speech/transcribe", payload);
            return response?.Text ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing audio");
            return "";
        }
    }

    public async Task<PracticeResponse?> GeneratePracticeExercisesAsync(PracticeRequest request)
    {
        try
        {
            return await SendWithAuthAsync<PracticeResponse>(HttpMethod.Post, "/api/practice/generate", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating practice exercises");
            return null;
        }
    }
}

public class TranscribeResponse 
{
    public string Text { get; set; } = "";
}
