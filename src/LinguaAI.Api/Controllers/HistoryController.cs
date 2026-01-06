using LinguaAI.Api.Services;
using LinguaAI.Common.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LinguaAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly MongoService _mongoService;

    public HistoryController(MongoService mongoService)
    {
        _mongoService = mongoService;
    }

    [HttpGet("action-logs")]
    public async Task<ActionResult<List<UserActionLog>>> GetActionLogs([FromQuery] string? userId, [FromQuery] int limit = 50)
    {
        var filter = userId != null 
            ? Builders<UserActionLog>.Filter.Eq(x => x.UserId, userId) 
            : Builders<UserActionLog>.Filter.Empty;

        var logs = await _mongoService.ActionLogs.Find(filter)
            .SortByDescending(x => x.Timestamp)
            .Limit(limit)
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("ai-logs")]
    public async Task<ActionResult<List<AIResponseLog>>> GetAILogs([FromQuery] string? userId, [FromQuery] int limit = 50)
    {
        var filter = userId != null 
            ? Builders<AIResponseLog>.Filter.Eq(x => x.UserId, userId) 
            : Builders<AIResponseLog>.Filter.Empty;

        var logs = await _mongoService.AILogs.Find(filter)
            .SortByDescending(x => x.Timestamp)
            .Limit(limit)
            .ToListAsync();

        return Ok(logs);
    }

    [HttpDelete("logs/{id}")]
    public async Task<IActionResult> DeleteLog(string id)
    {
        var resultAction = await _mongoService.ActionLogs.DeleteOneAsync(x => x.Id == id);
        var resultAI = await _mongoService.AILogs.DeleteOneAsync(x => x.Id == id);

        if (resultAction.DeletedCount > 0 || resultAI.DeletedCount > 0)
            return Ok("Deleted");
            
        return NotFound();
    }
}
