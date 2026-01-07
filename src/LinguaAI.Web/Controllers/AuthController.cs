using LinguaAI.Common.Models;
using LinguaAI.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace LinguaAI.Web.Controllers;

public class AuthController : Controller
{
    private readonly IApiService _apiService;

    public AuthController(IApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        var result = await _apiService.LoginAsync(model);
        if (result != null)
        {
            // Store session
            var data = JsonConvert.DeserializeObject<dynamic>(result);
            HttpContext.Session.SetString("UserId", (string)data.userId);
            HttpContext.Session.SetString("Username", (string)data.username);
            return RedirectToAction("Dashboard", "Home");
        }

        ModelState.AddModelError("", "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        if (await _apiService.RegisterAsync(model))
        {
            return RedirectToAction("Login");
        }

        ModelState.AddModelError("", "Registration failed.");
        return View(model);
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
