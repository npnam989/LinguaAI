using System.Text.Json.Serialization;

namespace LinguaAI.Web.Models;

// Vocabulary Models
public class VocabularyRequest
{
    public string Language { get; set; } = "ko";
    public string Theme { get; set; } = "greetings";
    public int Count { get; set; } = 10;
}

public class VocabularyResponse
{
    public List<VocabularyItem> Words { get; set; } = new();
}

public class VocabularyItem
{
    public string Word { get; set; } = "";
    public string Meaning { get; set; } = "";
    public string Pronunciation { get; set; } = "";
    public string Example { get; set; } = "";
}

// Translation Models
public class TranslateRequest
{
    public string Text { get; set; } = "";
    public string TargetLanguage { get; set; } = "en";
}

public class TranslateResponse
{
    public string Translated { get; set; } = "";
    public string OriginalLanguage { get; set; } = "";
}

// Theme Models
public class ThemeItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
}
