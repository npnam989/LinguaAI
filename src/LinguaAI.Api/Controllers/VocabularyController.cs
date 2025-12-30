using LinguaAI.Api.Models;
using LinguaAI.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinguaAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VocabularyController : ControllerBase
{
    private readonly IGeminiService _gemini;
    private readonly IFileParserService _fileParser;

    public VocabularyController(IGeminiService gemini, IFileParserService fileParser)
    {
        _gemini = gemini;
        _fileParser = fileParser;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<VocabularyResponse>> Generate([FromBody] VocabularyRequest request)
    {
        var words = await _gemini.GenerateVocabularyAsync(request.Language, request.Theme, request.Count);
        
        return Ok(new VocabularyResponse
        {
            Words = words.Select(w => new VocabularyItem
            {
                Word = w.word,
                Meaning = w.meaning,
                Pronunciation = w.pronunciation,
                Example = w.example
            }).ToList()
        });
    }

    /// <summary>
    /// Upload vocabulary from Excel (.xlsx) or Word (.docx) file
    /// Excel format: Column A=Word, B=Meaning, C=Pronunciation (optional), D=Example (optional)
    /// Word format: Word - Meaning (each line) or Word | Meaning | Pronunciation | Example
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<VocabularyResponse>> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Vui lòng chọn file" });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (extension != ".xlsx" && extension != ".docx")
        {
            return BadRequest(new { error = "Chỉ hỗ trợ file Excel (.xlsx) hoặc Word (.docx)" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            List<VocabularyItem> words;

            if (extension == ".xlsx")
            {
                words = _fileParser.ParseExcel(stream);
            }
            else
            {
                words = _fileParser.ParseWord(stream);
            }

            if (words.Count == 0)
            {
                return BadRequest(new { error = "Không tìm thấy từ vựng trong file. Vui lòng kiểm tra định dạng." });
            }

            string warning = "";
            
            // Limit to 100 words
            if (words.Count > 100)
            {
                words = words.Take(100).ToList();
                warning = "File quá lớn. Hệ thống chỉ xử lý 100 từ đầu tiên.";
            }

            // Check if enrichment is needed (missing pronunciation or example)
            var wordsToEnrich = words.Where(w => string.IsNullOrWhiteSpace(w.Pronunciation) || string.IsNullOrWhiteSpace(w.Example)).ToList();
            
            if (wordsToEnrich.Count > 0)
            {
                // Call AI to enrich
                var enrichInput = wordsToEnrich.Select(w => (w.Word, w.Meaning)).ToList();
                var enrichedData = await _gemini.EnrichVocabularyAsync(enrichInput);

                // Merge results
                foreach (var item in wordsToEnrich)
                {
                    var match = enrichedData.FirstOrDefault(e => e.word.Equals(item.Word, StringComparison.InvariantCultureIgnoreCase));
                    if (match != default)
                    {
                        if (string.IsNullOrWhiteSpace(item.Pronunciation)) item.Pronunciation = match.pronunciation;
                        if (string.IsNullOrWhiteSpace(item.Example)) item.Example = match.example;
                    }
                }
                
                if (!string.IsNullOrEmpty(warning)) warning += " ";
                warning += $"Đã tự động bổ sung thông tin cho {wordsToEnrich.Count} từ bằng AI.";
            }

            return Ok(new VocabularyResponse { Words = words, Warning = warning });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Lỗi đọc file: {ex.Message}" });
        }
    }

    [HttpGet("themes")]
    public ActionResult<List<object>> GetThemes()
    {
        var themes = new[]
        {
            new { Id = "greetings", Name = "Chào hỏi", Icon = "👋" },
            new { Id = "numbers", Name = "Số đếm", Icon = "🔢" },
            new { Id = "food", Name = "Đồ ăn", Icon = "🍕" },
            new { Id = "family", Name = "Gia đình", Icon = "👨‍👩‍👧‍👦" },
            new { Id = "colors", Name = "Màu sắc", Icon = "🎨" },
            new { Id = "animals", Name = "Động vật", Icon = "🐾" },
            new { Id = "weather", Name = "Thời tiết", Icon = "🌤️" },
            new { Id = "shopping", Name = "Mua sắm", Icon = "🛒" },
            new { Id = "travel", Name = "Du lịch", Icon = "🧳" },
            new { Id = "work", Name = "Công việc", Icon = "💼" },
            new { Id = "custom", Name = "📤 Tải file lên", Icon = "📤" }
        };
        return Ok(themes);
    }

    // Sample file downloads removed - now served as static files from Web project
    // See: /samples/vocabulary_template.xlsx and /samples/vocabulary_template.docx

    /// <summary>
    /// Translate text to target language using AI
    /// </summary>
    [HttpPost("translate")]
    public async Task<ActionResult> Translate([FromBody] TranslateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Text is required" });
        }

        var translated = await _gemini.TranslateAsync(request.Text, request.TargetLanguage ?? "en");
        return Ok(new { translated, originalLanguage = DetectLanguage(request.Text) });
    }

    private string DetectLanguage(string text)
    {
        // Simple language detection based on character ranges
        foreach (var c in text)
        {
            if (c >= 0xAC00 && c <= 0xD7AF) return "ko"; // Korean
            if (c >= 0x4E00 && c <= 0x9FFF) return "zh"; // Chinese
            if (c >= 0x0E00 && c <= 0x0E7F) return "th"; // Thai
        }
        
        // Check for Vietnamese diacritics
        if (text.Any(c => "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđĐ".Contains(c)))
            return "vi";
            
        return "en"; // Default to English
    }
}

public class TranslateRequest
{
    public string Text { get; set; } = "";
    public string? TargetLanguage { get; set; }
}
