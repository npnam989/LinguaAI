using LinguaAI.Common.Models;
using LinguaAI.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinguaAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReadingController : ControllerBase
{
    private readonly IGeminiService _gemini;

    public ReadingController(IGeminiService gemini)
    {
        _gemini = gemini;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ReadingResponse>> Generate([FromBody] ReadingRequest request)
    {
        var (title, content, vocabulary, questions) = await _gemini.GenerateReadingAsync(
            request.Language, request.Level, request.Topic);
        
        return Ok(new ReadingResponse
        {
            Title = title,
            Content = content,
            Vocabulary = vocabulary.Select(v => new VocabularyItem
            {
                Word = v.word,
                Meaning = v.meaning,
                Pronunciation = v.pronunciation,
                Example = v.example
            }).ToList(),
            Questions = questions.Select(q => new QuizQuestion
            {
                Question = q.question,
                Options = q.options,
                CorrectIndex = q.correctIndex
            }).ToList()
        });
    }

    [HttpGet("topics")]
    public ActionResult<List<object>> GetTopics()
    {
        var topics = new[]
        {
            new { Id = "culture", Name = "Văn hóa", Icon = "🏛️" },
            new { Id = "food", Name = "Ẩm thực", Icon = "🍜" },
            new { Id = "travel", Name = "Du lịch", Icon = "✈️" },
            new { Id = "technology", Name = "Công nghệ", Icon = "💻" },
            new { Id = "nature", Name = "Thiên nhiên", Icon = "🌿" },
            new { Id = "sports", Name = "Thể thao", Icon = "⚽" },
            new { Id = "history", Name = "Lịch sử", Icon = "📜" },
            new { Id = "daily", Name = "Đời sống", Icon = "🏠" }
        };
        return Ok(topics);
    }
}
