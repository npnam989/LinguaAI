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
        ViewBag.ApiBaseUrl = _config["ApiBaseUrl"] ?? "http://localhost:5000";
        ViewBag.AuthUserId = _config["Auth:UserId"] ?? "";
        ViewBag.AuthApiKey = _config["Auth:ApiKey"] ?? "";
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
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
    {
        var result = await _apiService.TranslateAsync(request.Text, request.TargetLanguage);
        
        if (result == null)
            return StatusCode(500, new { error = "Failed to translate" });
        
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetThemes()
    {
        var themes = await _apiService.GetThemesAsync();
        return Json(themes);
    }
}
