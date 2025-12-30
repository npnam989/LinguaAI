using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LinguaAI.Web.Models;

namespace LinguaAI.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Dashboard()
    {
        ViewBag.ApiBaseUrl = "https://linguaai.up.railway.app";
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
