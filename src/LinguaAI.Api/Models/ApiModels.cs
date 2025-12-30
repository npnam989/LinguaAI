namespace LinguaAI.Api.Models;

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

public class PronunciationRequest
{
    public string Language { get; set; } = "en";
    public string TargetText { get; set; } = string.Empty;
    public string SpokenText { get; set; } = string.Empty;
}

public class PronunciationResponse
{
    public int Score { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public List<string> Corrections { get; set; } = new();
}

public class WritingRequest
{
    public string Language { get; set; } = "en";
    public string Text { get; set; } = string.Empty;
    public string Level { get; set; } = "intermediate";
}

public class WritingResponse
{
    public string CorrectedText { get; set; } = string.Empty;
    public List<WritingCorrection> Corrections { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

public class WritingCorrection
{
    public string Original { get; set; } = string.Empty;
    public string Corrected { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

public class ReadingRequest
{
    public string Language { get; set; } = "en";
    public string Level { get; set; } = "intermediate";
    public string? Topic { get; set; }
}

public class ReadingResponse
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<VocabularyItem> Vocabulary { get; set; } = new();
    public List<QuizQuestion> Questions { get; set; } = new();
}

public class QuizQuestion
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectIndex { get; set; }
}

public class VocabularyRequest
{
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "daily";
    public int Count { get; set; } = 10;
}

public class VocabularyResponse
{
    public List<VocabularyItem> Words { get; set; } = new();
    public string Warning { get; set; } = string.Empty;
}

public class VocabularyItem
{
    public string Word { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Pronunciation { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
}
