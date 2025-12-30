using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LinguaAI.Api.Models;

namespace LinguaAI.Api.Services;

public interface IFileParserService
{
    List<VocabularyItem> ParseExcel(Stream stream);
    List<VocabularyItem> ParseWord(Stream stream);
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
}
