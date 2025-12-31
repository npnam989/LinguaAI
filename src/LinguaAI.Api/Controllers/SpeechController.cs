using LinguaAI.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinguaAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpeechController : ControllerBase
{
    private readonly IGeminiService _gemini;
    private readonly ILogger<SpeechController> _logger;

    public SpeechController(IGeminiService gemini, ILogger<SpeechController> logger)
    {
        _gemini = gemini;
        _logger = logger;
    }

    /// <summary>
    /// Transcribe audio to text using AI
    /// </summary>
    [HttpPost("transcribe")]
    public async Task<ActionResult<TranscribeResponse>> Transcribe([FromForm] IFormFile audio, [FromForm] string language = "ko")
    {
        try
        {
            if (audio == null || audio.Length == 0)
            {
                return BadRequest(new { error = "No audio file provided" });
            }

            // Read audio data
            using var memoryStream = new MemoryStream();
            await audio.CopyToAsync(memoryStream);
            var audioData = memoryStream.ToArray();

            // Determine MIME type
            var mimeType = audio.ContentType ?? "audio/wav";
            if (string.IsNullOrEmpty(mimeType) || mimeType == "application/octet-stream")
            {
                // Guess from extension
                var ext = Path.GetExtension(audio.FileName)?.ToLower();
                mimeType = ext switch
                {
                    ".wav" => "audio/wav",
                    ".mp3" => "audio/mp3",
                    ".webm" => "audio/webm",
                    ".ogg" => "audio/ogg",
                    ".m4a" => "audio/mp4",
                    _ => "audio/wav"
                };
            }

            _logger.LogInformation("Transcribing audio: {Length} bytes, {MimeType}, Lang: {Language}", 
                audioData.Length, mimeType, language);

            // Transcribe using Gemini
            var transcript = await _gemini.TranscribeAudioAsync(audioData, language, mimeType);

            if (string.IsNullOrEmpty(transcript))
            {
                return Ok(new TranscribeResponse 
                { 
                    Transcript = "", 
                    Success = false, 
                    Error = "Could not transcribe audio" 
                });
            }

            return Ok(new TranscribeResponse 
            { 
                Transcript = transcript, 
                Success = true 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing audio");
            return StatusCode(500, new { error = "Transcription failed", message = ex.Message });
        }
    }
}

public class TranscribeResponse
{
    public string Transcript { get; set; } = "";
    public bool Success { get; set; }
    public string? Error { get; set; }
}
