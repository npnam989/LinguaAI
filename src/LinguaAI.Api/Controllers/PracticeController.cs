using LinguaAI.Api.Services;
using LinguaAI.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace LinguaAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PracticeController : ControllerBase
{
    private readonly IGeminiService _geminiService;

    public PracticeController(IGeminiService geminiService)
    {
        _geminiService = geminiService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] PracticeRequest request)
    {
        var result = await _geminiService.GeneratePracticeExercisesAsync(request);
        return Ok(result);
    }
}
