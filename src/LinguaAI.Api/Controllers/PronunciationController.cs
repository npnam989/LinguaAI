using LinguaAI.Api.Models;
using LinguaAI.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinguaAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PronunciationController : ControllerBase
{
    private readonly IGeminiService _gemini;

    public PronunciationController(IGeminiService gemini)
    {
        _gemini = gemini;
    }

    [HttpPost("evaluate")]
    public async Task<ActionResult<PronunciationResponse>> Evaluate([FromBody] PronunciationRequest request)
    {
        var (score, feedback, corrections, words) = await _gemini.EvaluatePronunciationAsync(request.Language, request.TargetText, request.SpokenText);

        return Ok(new PronunciationResponse
        {
            Score = score,
            Feedback = feedback,
            Corrections = corrections,
            Words = words.Select(w => new PronunciationWordResult
            {
                Word = w.word,
                Correct = w.correct,
                Error = w.error
            }).ToList()
        });
    }

    [HttpGet("phrases/{language}")]
    public ActionResult<List<object>> GetPhrases(string language)
    {
        var phrases = language switch
        {
            "ko" => new[]
            {
                new { Text = "안녕하세요", Romanization = "annyeonghaseyo", Meaning = "Xin chào" },
                new { Text = "감사합니다", Romanization = "gamsahamnida", Meaning = "Cảm ơn" },
                new { Text = "죄송합니다", Romanization = "joesonghamnida", Meaning = "Xin lỗi" },
                new { Text = "사랑해요", Romanization = "saranghaeyo", Meaning = "Tôi yêu bạn" },
                new { Text = "맛있어요", Romanization = "masisseoyo", Meaning = "Ngon quá" }
            },
            "zh" => new[]
            {
                new { Text = "你好", Romanization = "nǐ hǎo", Meaning = "Xin chào" },
                new { Text = "谢谢", Romanization = "xiè xiè", Meaning = "Cảm ơn" },
                new { Text = "对不起", Romanization = "duì bù qǐ", Meaning = "Xin lỗi" },
                new { Text = "我爱你", Romanization = "wǒ ài nǐ", Meaning = "Tôi yêu bạn" },
                new { Text = "很好吃", Romanization = "hěn hǎo chī", Meaning = "Ngon quá" }
            },
            _ => new[]
            {
                new { Text = "Hello", Romanization = "", Meaning = "Xin chào" },
                new { Text = "Thank you", Romanization = "", Meaning = "Cảm ơn" },
                new { Text = "I'm sorry", Romanization = "", Meaning = "Xin lỗi" },
                new { Text = "I love you", Romanization = "", Meaning = "Tôi yêu bạn" },
                new { Text = "Delicious!", Romanization = "", Meaning = "Ngon quá" }
            }
        };
        return Ok(phrases);
    }
}
