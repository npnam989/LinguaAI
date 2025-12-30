using Microsoft.AspNetCore.Mvc;

namespace LinguaAI.Web.Controllers;

public class PracticeController : Controller
{
    private readonly IConfiguration _config;

    public PracticeController(IConfiguration config)
    {
        _config = config;
    }

    private void SetApiBaseUrl()
    {
        ViewBag.ApiBaseUrl = _config["ApiBaseUrl"] ?? "http://localhost:5000";
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
}
