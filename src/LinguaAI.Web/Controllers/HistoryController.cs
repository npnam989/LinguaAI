using LinguaAI.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinguaAI.Web.Controllers;

public class HistoryController : Controller
{
    private readonly IApiService _apiService;

    public HistoryController(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var actionLogs = await _apiService.GetActionLogsAsync(userId);
        var aiLogs = await _apiService.GetAILogsAsync(userId);

        ViewBag.ActionLogs = actionLogs ?? new();
        ViewBag.AILogs = aiLogs ?? new();
        
        return View();
    }
}
