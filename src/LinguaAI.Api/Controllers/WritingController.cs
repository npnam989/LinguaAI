using LinguaAI.Api.Models;
using LinguaAI.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinguaAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WritingController : ControllerBase
{
    private readonly IGeminiService _gemini;

    public WritingController(IGeminiService gemini)
    {
        _gemini = gemini;
    }

    [HttpPost("check")]
    public async Task<ActionResult<WritingResponse>> Check([FromBody] WritingRequest request)
    {
        var (corrected, corrections, suggestions) = await _gemini.CheckWritingAsync(
            request.Language, request.Text, request.Level);
        
        return Ok(new WritingResponse
        {
            CorrectedText = corrected,
            Corrections = corrections.Select(c => new WritingCorrection
            {
                Original = c.orig,
                Corrected = c.fix,
                Explanation = c.explanation
            }).ToList(),
            Suggestions = suggestions
        });
    }

    [HttpGet("prompts/{language}")]
    public ActionResult<List<object>> GetPrompts(string language)
    {
        var prompts = new[]
        {
            new { Level = "beginner", Title = "Giới thiệu bản thân", Description = "Viết một đoạn ngắn giới thiệu về bản thân" },
            new { Level = "beginner", Title = "Gia đình", Description = "Mô tả các thành viên trong gia đình" },
            new { Level = "intermediate", Title = "Sở thích", Description = "Viết về sở thích và hoạt động yêu thích" },
            new { Level = "intermediate", Title = "Du lịch", Description = "Kể về một chuyến du lịch đáng nhớ" },
            new { Level = "advanced", Title = "Quan điểm", Description = "Nêu quan điểm về một vấn đề xã hội" },
            new { Level = "advanced", Title = "Kế hoạch", Description = "Mô tả kế hoạch tương lai của bạn" }
        };
        return Ok(prompts);
    }
}
