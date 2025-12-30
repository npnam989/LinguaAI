using LinguaAI.Web.Models;
using LinguaAI.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinguaAI.Web.Controllers;

public class PracticeController : Controller
{
    private readonly IConfiguration _config;
    private readonly IApiService _apiService;

    public PracticeController(IConfiguration config, IApiService apiService)
    {
        _config = config;
        _apiService = apiService;
    }

    private void SetApiBaseUrl()
    {
        // Auth credentials are now only used server-side in ApiService
        // No longer exposed to frontend
    }

    public IActionResult Conversation()
    {
        SetApiBaseUrl();
        return View();
    }

    public IActionResult Pronunciation()
    {
        SetApiBaseUrl();
        return View();
    }

    public IActionResult Writing()
    {
        SetApiBaseUrl();
        return View();
    }

    public IActionResult Reading()
    {
        SetApiBaseUrl();
        return View();
    }

    public IActionResult Vocabulary()
    {
        SetApiBaseUrl();
        return View();
    }

    // ==================== API Endpoints ====================

    [HttpPost]
    public async Task<IActionResult> GenerateVocabulary([FromBody] VocabularyRequest request)
    {
        var result = await _apiService.GenerateVocabularyAsync(
            request.Language, 
            request.Theme, 
            request.Count);
        
        if (result == null)
            return StatusCode(500, new { error = "Failed to generate vocabulary" });
        
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> UploadVocabulary(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        using var stream = file.OpenReadStream();
        var result = await _apiService.UploadVocabularyFileAsync(stream, file.FileName);
        
        if (result == null)
            return StatusCode(500, new { error = "Failed to process file" });
        
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateReading([FromBody] ReadingRequest request)
    {
        var result = await _apiService.GenerateReadingAsync(request.Language, request.Level, request.Topic);
        
        if (result == null)
            return StatusCode(500, new { error = "Failed to generate reading" });
        
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> EvaluatePronunciation([FromBody] PronunciationRequest request)
    {
        var result = await _apiService.EvaluatePronunciationAsync(request.Language, request.TargetText, request.SpokenText);
        
        if (result == null)
            return StatusCode(500, new { error = "Failed to evaluate pronunciation" });
        
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> CheckWriting([FromBody] WritingRequest request)
    {
        var result = await _apiService.CheckWritingAsync(request.Language, request.Text, request.Level);
        
        if (result == null)
            return StatusCode(500, new { error = "Failed to check writing" });
        
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
    {
        var result = await _apiService.TranslateAsync(request.Text, request.TargetLanguage);
        
        if (result == null)
            return StatusCode(500, new { error = "Failed to translate" });
        
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var result = await _apiService.ChatAsync(request.Language, request.Scenario, request.Message, request.History);
        if (result == null) return StatusCode(500, new { error = "Failed to chat" });
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetPhrases(string language)
    {
        var result = await _apiService.GetPhrasesAsync(language);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetThemes()
    {
        var themes = await _apiService.GetThemesAsync();
        return Json(themes);
    }
}
