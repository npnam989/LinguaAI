using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LinguaAI.Api.Models;

namespace LinguaAI.Api.Services;

public interface IFileParserService
{
    List<VocabularyItem> ParseExcel(Stream stream);
    List<VocabularyItem> ParseWord(Stream stream);
    byte[] GenerateSampleExcel();
    byte[] GenerateSampleWord();
}

public class FileParserService : IFileParserService
{
    /// <summary>
    /// Parse Excel file. Expected format:
    /// Column A: Word, Column B: Meaning, Column C: Pronunciation (optional), Column D: Example (optional)
    /// </summary>
    public List<VocabularyItem> ParseExcel(Stream stream)
    {
        var result = new List<VocabularyItem>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        // Find last row with data
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        // Skip header row (start from row 2)
        for (int row = 2; row <= lastRow; row++)
        {
            var word = worksheet.Cell(row, 1).GetString()?.Trim();
            var meaning = worksheet.Cell(row, 2).GetString()?.Trim();

            if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(meaning))
                continue;

            result.Add(new VocabularyItem
            {
                Word = word,
                Meaning = meaning,
                Pronunciation = worksheet.Cell(row, 3).GetString()?.Trim() ?? "",
                Example = worksheet.Cell(row, 4).GetString()?.Trim() ?? ""
            });
        }

        return result;
    }

    /// <summary>
    /// Parse Word file. Expected format per line:
    /// Word - Meaning
    /// or
    /// Word | Meaning | Pronunciation | Example
    /// </summary>
    public List<VocabularyItem> ParseWord(Stream stream)
    {
        var result = new List<VocabularyItem>();

        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;

        if (body == null)
            return result;

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            var text = paragraph.InnerText?.Trim();
            if (string.IsNullOrEmpty(text))
                continue;

            VocabularyItem? item = null;

            // Try pipe separator first (Word | Meaning | Pronunciation | Example)
            if (text.Contains('|'))
            {
                var parts = text.Split('|').Select(p => p.Trim()).ToArray();
                if (parts.Length >= 2)
                {
                    item = new VocabularyItem
                    {
                        Word = parts[0],
                        Meaning = parts[1],
                        Pronunciation = parts.Length > 2 ? parts[2] : "",
                        Example = parts.Length > 3 ? parts[3] : ""
                    };
                }
            }
            // Try dash separator (Word - Meaning)
            else if (text.Contains(" - "))
            {
                var parts = text.Split(" - ", 2);
                if (parts.Length == 2)
                {
                    item = new VocabularyItem
                    {
                        Word = parts[0].Trim(),
                        Meaning = parts[1].Trim(),
                        Pronunciation = "",
                        Example = ""
                    };
                }
            }
            // Try tab separator
            else if (text.Contains('\t'))
            {
                var parts = text.Split('\t').Select(p => p.Trim()).ToArray();
                if (parts.Length >= 2)
                {
                    item = new VocabularyItem
                    {
                        Word = parts[0],
                        Meaning = parts[1],
                        Pronunciation = parts.Length > 2 ? parts[2] : "",
                        Example = parts.Length > 3 ? parts[3] : ""
                    };
                }
            }

            if (item != null && !string.IsNullOrEmpty(item.Word) && !string.IsNullOrEmpty(item.Meaning))
            {
                result.Add(item);
            }
        }

        return result;
    }

    /// <summary>
    /// Generate sample Excel file with example vocabulary
    /// </summary>
    public byte[] GenerateSampleExcel()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Vocabulary");

        // Headers
        worksheet.Cell(1, 1).Value = "Từ vựng";
        worksheet.Cell(1, 2).Value = "Nghĩa";
        worksheet.Cell(1, 3).Value = "Phát âm";
        worksheet.Cell(1, 4).Value = "Ví dụ";

        // Style headers
        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Sample data
        var samples = new[]
        {
            ("안녕하세요", "Xin chào", "annyeonghaseyo", "안녕하세요! 만나서 반갑습니다."),
            ("감사합니다", "Cảm ơn", "gamsahamnida", "도와주셔서 감사합니다."),
            ("사랑해요", "Tôi yêu bạn", "saranghaeyo", "엄마, 사랑해요!"),
            ("맛있어요", "Ngon quá", "masisseoyo", "이 음식은 정말 맛있어요."),
            ("괜찮아요", "Không sao", "gwaenchanayo", "괜찮아요, 걱정 마세요.")
        };

        for (int i = 0; i < samples.Length; i++)
        {
            worksheet.Cell(i + 2, 1).Value = samples[i].Item1;
            worksheet.Cell(i + 2, 2).Value = samples[i].Item2;
            worksheet.Cell(i + 2, 3).Value = samples[i].Item3;
            worksheet.Cell(i + 2, 4).Value = samples[i].Item4;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Generate sample Word file with example vocabulary
    /// </summary>
    public byte[] GenerateSampleWord()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Title
            var titlePara = body.AppendChild(new Paragraph());
            var titleRun = titlePara.AppendChild(new Run());
            titleRun.AppendChild(new Text("Danh sách từ vựng mẫu"));
            titleRun.RunProperties = new RunProperties(new Bold());

            // Instructions
            var instructPara = body.AppendChild(new Paragraph());
            instructPara.AppendChild(new Run(new Text("Định dạng: Từ | Nghĩa | Phát âm | Ví dụ (hoặc Từ - Nghĩa)")));

            // Empty line
            body.AppendChild(new Paragraph());

            // Sample data
            var samples = new[]
            {
                "안녕하세요 | Xin chào | annyeonghaseyo | 안녕하세요! 만나서 반갑습니다.",
                "감사합니다 | Cảm ơn | gamsahamnida | 도와주셔서 감사합니다.",
                "사랑해요 | Tôi yêu bạn | saranghaeyo | 엄마, 사랑해요!",
                "맛있어요 | Ngon quá | masisseoyo | 이 음식은 정말 맛있어요.",
                "괜찮아요 | Không sao | gwaenchanayo | 괜찮아요, 걱정 마세요."
            };

            foreach (var sample in samples)
            {
                var para = body.AppendChild(new Paragraph());
                para.AppendChild(new Run(new Text(sample)));
            }

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }
}
