using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using LinguaAI.Common.Models;

namespace LinguaAI.Desktop.Services;

public class LinguaApiService
{
    private readonly HttpClient _httpClient;
    public static string BaseUrl { get; set; } = "http://localhost:5278"; // Default, updated by App.xaml.cs
    private const long WINDOW_TICKS = 600_000_000; // 60 seconds

    public LinguaApiService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    private void AddAuthHeader()
    {
        // Read from the Environment Variables set by App.xaml.cs on startup
        var userId = Environment.GetEnvironmentVariable("Auth__UserId");
        var apiKey = Environment.GetEnvironmentVariable("Auth__ApiKey");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(apiKey)) return;

        var ticks = DateTime.UtcNow.Ticks;
        var window = ticks / WINDOW_TICKS;
        var password = ComputeSha256(FormattableString.Invariant($"{apiKey}{window}"));
        var hash = ComputeSha256(FormattableString.Invariant($"{userId}:{password}"));

        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"HMAC-SHA256 {userId}:{hash}");
    }

    private static string ComputeSha256(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<PronunciationResponse?> EvaluatePronunciationAsync(string language, string target, string spoken)
    {
        try
        {
            AddAuthHeader();
            var request = new PronunciationRequest { Language = language, TargetText = target, SpokenText = spoken };
            var response = await _httpClient.PostAsJsonAsync("/api/pronunciation/evaluate", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PronunciationResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ThemeItem>> GetThemesAsync()
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.GetAsync("/api/vocabulary/themes");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ThemeItem>>() ?? new List<ThemeItem>();
            }
            return new List<ThemeItem>();
        }
        catch
        {
            return new List<ThemeItem>();
        }
    }

    public async Task<VocabularyResponse?> GenerateVocabularyAsync(string language, string theme, int count)
    {
        try
        {
            AddAuthHeader();
            var request = new VocabularyRequest { Language = language, Theme = theme, Count = count };
            var response = await _httpClient.PostAsJsonAsync("/api/vocabulary/generate", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<VocabularyResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> ChatAsync(string language, string scenario, string message, List<ChatMessage> history)
    {
        try
        {
            AddAuthHeader();
            var request = new ChatRequest 
            { 
                Language = language, 
                Scenario = scenario, 
                Message = message, 
                History = history 
            };
            
            var response = await _httpClient.PostAsJsonAsync("/api/conversation/chat", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
                return result?.Reply ?? "Sorry, I didn't catch that.";
            }
            return "Error calling API.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<ReadingResponse?> GenerateReadingAsync(string language, string level, string? topic)
    {
        try
        {
            AddAuthHeader();
            var request = new ReadingRequest { Language = language, Level = level, Topic = topic };
            var response = await _httpClient.PostAsJsonAsync("/api/reading/generate", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ReadingResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<WritingResponse?> CheckWritingAsync(string language, string text, string level)
    {
        try
        {
            AddAuthHeader();
            var request = new WritingRequest { Language = language, Text = text, Level = level };
            var response = await _httpClient.PostAsJsonAsync("/api/writing/check", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<WritingResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
