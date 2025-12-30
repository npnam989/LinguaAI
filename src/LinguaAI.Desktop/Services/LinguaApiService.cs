using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using LinguaAI.Common.Models;

namespace LinguaAI.Desktop.Services;

public class LinguaApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5000"; // Local self-hosted API

    public LinguaApiService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    public async Task<PronunciationResponse?> EvaluatePronunciationAsync(string language, string target, string spoken)
    {
        try
        {
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
}
