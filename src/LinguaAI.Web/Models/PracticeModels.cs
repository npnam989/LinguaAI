using System.Collections.Generic;

namespace LinguaAI.Web.Models;

public class PracticeRequest
{
    public List<string> Words { get; set; } = new();
    public string Language { get; set; } = "en";
    public string Level { get; set; } = "Elementary"; // "Elementary", "Intermediate", "Advanced"
    public string Type { get; set; } = "fill_blank"; // "fill_blank", "arrange", "translate"
    public int Count { get; set; } = 5;
}

public class PracticeExercise
{
    public string Question { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public List<string> Options { get; set; } = new(); // For multiple choice or arrange words
    public string Explanation { get; set; } = "";
    public string ExplanationVi { get; set; } = ""; // Vietnamese explanation
    public string TargetWord { get; set; } = ""; // The key vocabulary word being practiced
}

public class PracticeResponse
{
    public List<PracticeExercise> Exercises { get; set; } = new();
}
