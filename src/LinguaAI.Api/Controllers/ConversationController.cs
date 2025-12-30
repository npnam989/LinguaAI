using LinguaAI.Common.Models;
using LinguaAI.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinguaAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationController : ControllerBase
{
    private readonly IGeminiService _gemini;

    public ConversationController(IGeminiService gemini)
    {
        _gemini = gemini;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        var history = request.History.Select(h => (h.Role, h.Content)).ToList();
        var reply = await _gemini.ChatAsync(request.Language, request.Scenario, request.Message, history);
        
        // Split reply and translation if available
        var parts = reply.Split("\n\n", 2);
        return Ok(new ChatResponse
        {
            Reply = parts[0],
            Translation = parts.Length > 1 ? parts[1] : null
        });
    }

    [HttpGet("scenarios")]
    public ActionResult<List<object>> GetScenarios()
    {
        var scenarios = new[]
        {
            new { Id = "coffee", Name = "Quán cà phê", Description = "Gọi đồ uống tại quán" },
            new { Id = "restaurant", Name = "Nhà hàng", Description = "Đặt món và thanh toán" },
            new { Id = "shopping", Name = "Mua sắm", Description = "Trao đổi với nhân viên bán hàng" },
            new { Id = "travel", Name = "Du lịch", Description = "Hỏi đường, đặt phòng khách sạn" },
            new { Id = "work", Name = "Công việc", Description = "Họp, thuyết trình, email" },
            new { Id = "daily", Name = "Đời thường", Description = "Trò chuyện bạn bè, gia đình" },
            new { Id = "interview", Name = "Phỏng vấn", Description = "Phỏng vấn xin việc" }
        };
        return Ok(scenarios);
    }
}
