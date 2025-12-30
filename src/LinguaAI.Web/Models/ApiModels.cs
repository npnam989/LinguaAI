using System.Text.Json.Serialization;

namespace LinguaAI.Web.Models;

// Vocabulary Models
public class ChatRequest
{
    public string Language { get; set; } = "en";
    public string Scenario { get; set; } = "general";
    public string Message { get; set; } = string.Empty;
    public List<ChatMessage> History { get; set; } = new();
}

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public string? Translation { get; set; }
}

public class VocabularyRequest
{
    public string Language { get; set; } = "ko";
    public string Theme { get; set; } = "greetings";
    public int Count { get; set; } = 10;
}

public class VocabularyResponse
{
    public List<VocabularyItem> Words { get; set; } = new();
    public string Warning { get; set; } = string.Empty;
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

// Reading Models
public class ReadingRequest
{
    public string Language { get; set; } = "en";
    public string Level { get; set; } = "intermediate";
    public string? Topic { get; set; }
}

public class ReadingResponse
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public List<VocabularyItem> Vocabulary { get; set; } = new();
    public List<QuizQuestion> Questions { get; set; } = new();
}

public class QuizQuestion
{
    public string Question { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public int CorrectIndex { get; set; }
}

// Writing Models
public class WritingRequest
{
    public string Language { get; set; } = "en";
    public string Text { get; set; } = "";
    public string Level { get; set; } = "intermediate";
}

public class WritingResponse
{
    public string CorrectedText { get; set; } = "";
    public List<WritingCorrection> Corrections { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

public class WritingCorrection
{
    public string Original { get; set; } = "";
    public string Corrected { get; set; } = "";
    public string Explanation { get; set; } = "";
}

// Pronunciation Models
public class PronunciationRequest
{
    public string Language { get; set; } = "en";
    public string TargetText { get; set; } = "";
    public string SpokenText { get; set; } = "";
}

public class PronunciationResponse
{
    public int Score { get; set; }
    public string Feedback { get; set; } = "";
    public List<string> Corrections { get; set; } = new();
    public List<PronunciationWordResult> Words { get; set; } = new();
}

public class PronunciationWordResult
{
    public string Word { get; set; } = "";
    public bool Correct { get; set; }
    public string? Error { get; set; }
}

// Theme Models
public class ThemeItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
}

public class PronunciationPhrase
{
    public string Text { get; set; } = "";
    public string Romanization { get; set; } = "";
    public string Meaning { get; set; } = "";
}
