using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LinguaAI.Api.Services;

public interface IGeminiService
{
    Task<string> ChatAsync(string language, string scenario, string message, List<(string role, string content)> history);
    Task<(int score, string feedback, List<string> corrections, List<(string word, bool correct, string error)> words)> EvaluatePronunciationAsync(string language, string target, string spoken);
    Task<(string corrected, List<(string orig, string fix, string explanation)> corrections, List<string> suggestions)> CheckWritingAsync(string language, string text, string level);
    Task<(string title, string content, List<(string word, string meaning, string pronunciation, string example)> vocabulary, List<(string question, List<string> options, int correctIndex, string explanation)> quiz)> GenerateReadingAsync(string language, string level, string? topic);
    Task<List<(string word, string meaning, string pronunciation, string example)>> GenerateVocabularyAsync(string language, string theme, int count);
    Task<string> TranslateAsync(string text, string targetLanguage);
    Task<List<(string word, string meaning, string pronunciation, string example)>> EnrichVocabularyAsync(List<(string word, string meaning)> items);
    Task<string> TranscribeAudioAsync(byte[] audioData, string language, string mimeType = "audio/wav");
    Task<LinguaAI.Common.Models.PracticeResponse> GeneratePracticeExercisesAsync(LinguaAI.Common.Models.PracticeRequest request);
    Task<LinguaAI.Common.Models.TranslationCheckResponse> CheckTranslationAsync(LinguaAI.Common.Models.TranslationCheckRequest request);
}



public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GeminiService> _logger;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        { "en", "English" },
        { "ko", "Korean" },
        { "zh", "Chinese (Mandarin)" }
    };

    public GeminiService(IConfiguration config, ILogger<GeminiService> logger)
    {
        _logger = logger;
        _apiKey = config["Gemini:ApiKey"] ?? "";
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Gemini API key not configured. Set environment variable: Gemini__ApiKey");
        }
        
        _httpClient = new HttpClient();
    }

    private async Task<string> CallGeminiAsync(string prompt)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var json = JsonConvert.SerializeObject(requestBody);
        
        // Retry with exponential backoff for rate limiting
        int maxRetries = 3;
        int delayMs = 2000; // Start with 2 seconds

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}?key={_apiKey}", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var doc = JObject.Parse(responseText);
                var text = doc["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                return text ?? "";
            }

            // Check if rate limited
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt < maxRetries)
                {
                    _logger.LogWarning("Rate limited, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})", 
                        delayMs, attempt + 1, maxRetries);
                    await Task.Delay(delayMs);
                    delayMs *= 2; // Exponential backoff
                    continue;
                }
            }

            _logger.LogError("Gemini API error: {Response}", responseText);
            throw new Exception($"Gemini API error: {response.StatusCode}. Vui lòng thử lại sau ít phút.");
        }

        throw new Exception("Không thể kết nối Gemini API sau nhiều lần thử.");
    }

    public async Task<string> ChatAsync(string language, string scenario, string message, List<(string role, string content)> history)
    {
        var langName = GetLanguageName(language);
        var systemPrompt = $@"You are a friendly native {langName} speaker helping someone practice {langName} conversation.
Scenario: {scenario}
Rules:
- Respond ONLY in {langName}
- Keep responses conversational and natural
- If the user makes mistakes, gently correct them
- Adapt to the user's level
- After your response, add a line break and provide a brief translation in Vietnamese";

        var prompt = BuildConversationPrompt(systemPrompt, history, message);
        return await CallGeminiAsync(prompt);
    }

    public async Task<(int score, string feedback, List<string> corrections, List<(string word, bool correct, string error)> words)> EvaluatePronunciationAsync(string language, string target, string spoken)
    {
        var langName = GetLanguageName(language);
        var prompt = $@"You are a {langName} pronunciation expert. Compare the target text with what was spoken.
Analyze the pronunciation of EACH word.

Target: {target}
Spoken: {spoken}

Respond in JSON format only (no markdown):
{{
    ""score"": <0-100 overall score>,
    ""feedback"": ""<brief feedback in VietnameseSummary>"",
    ""corrections"": [""<general tips in Vietnamese>""],
    ""words"": [
        {{ ""word"": ""<target word>"", ""correct"": true/false, ""error"": ""<brief error description if false>"" }}
    ]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            var json = ExtractJson(response);
            var root = JObject.Parse(json);
            
            var words = new List<(string, bool, string)>();
            var wordsEl = root["words"] as JArray;
            if (wordsEl != null)
            {
                words = wordsEl.Select(x => (
                    x["word"]?.ToString() ?? "",
                    x["correct"]?.Value<bool>() ?? false,
                    x["error"]?.ToString() ?? ""
                )).ToList();
            }

            return (
                root["score"]?.Value<int>() ?? 0,
                root["feedback"]?.ToString() ?? "",
                (root["corrections"] as JArray)?.Select(x => x.ToString()).ToList() ?? new List<string>(),
                words
            );
        }
        catch
        {
            return (50, "Không thể đánh giá chi tiết", new List<string>(), new List<(string, bool, string)>());
        }
    }

    public async Task<(string corrected, List<(string orig, string fix, string explanation)> corrections, List<string> suggestions)> CheckWritingAsync(string language, string text, string level)
    {
        var langName = GetLanguageName(language);
        var prompt = $@"You are a {langName} writing tutor. Check the following text for grammar, spelling, and style.
Level: {level}
Text: {text}

Respond in JSON format only (no markdown):
{{
    ""correctedText"": ""<corrected version>"",
    ""corrections"": [
        {{""original"": ""<wrong part>"", ""corrected"": ""<fixed part>"", ""explanation"": ""<explanation in Vietnamese>""}}
    ],
    ""suggestions"": [""<improvement suggestions in Vietnamese>""]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            var json = ExtractJson(response);
            var root = JObject.Parse(json);
            return (
                root["correctedText"]?.ToString() ?? text,
                (root["corrections"] as JArray)?.Select(x => (
                    x["original"]?.ToString() ?? "",
                    x["corrected"]?.ToString() ?? "",
                    x["explanation"]?.ToString() ?? ""
                )).ToList() ?? new List<(string, string, string)>(),
                (root["suggestions"] as JArray)?.Select(x => x.ToString()).ToList() ?? new List<string>()
            );
        }
        catch
        {
            return (text, new List<(string, string, string)>(), new List<string>());
        }
    }

    public async Task<(string title, string content, List<(string word, string meaning, string pronunciation, string example)> vocabulary, List<(string question, List<string> options, int correctIndex, string explanation)> quiz)> GenerateReadingAsync(string language, string level, string? topic)
    {
        var langName = GetLanguageName(language);
        var topicPart = string.IsNullOrEmpty(topic) ? "any interesting topic" : topic;
        var prompt = $@"Generate a short reading passage in {langName} for {level} level learners about {topicPart}.

Respond in JSON format only (no markdown):
{{
    ""title"": ""<title in {langName}>"",
    ""content"": ""<3-5 paragraphs in {langName}>"",
    ""vocabulary"": [
        {{""word"": ""<word>"", ""meaning"": ""<Vietnamese meaning>"", ""pronunciation"": ""<romanization if applicable>"", ""example"": ""<example sentence>""}}
    ],
    ""questions"": [
        {{""question"": ""<comprehension question in Vietnamese>"", ""options"": [""A"", ""B"", ""C"", ""D""], ""correctIndex"": 0, ""explanation"": ""<explanation why is it correct in Vietnamese>""}}
    ]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            var json = ExtractJson(response);
            var root = JObject.Parse(json);
            return (
                root["title"]?.ToString() ?? "",
                root["content"]?.ToString() ?? "",
                (root["vocabulary"] as JArray)?.Select(x => (
                    x["word"]?.ToString() ?? "",
                    x["meaning"]?.ToString() ?? "",
                    x["pronunciation"]?.ToString() ?? "",
                    x["example"]?.ToString() ?? ""
                )).ToList() ?? new List<(string, string, string, string)>(),
                (root["questions"] as JArray)?.Select(x => (
                    x["question"]?.ToString() ?? "",
                    (x["options"] as JArray)?.Select(o => o.ToString()).ToList() ?? new List<string>(),
                    x["correctIndex"]?.Value<int>() ?? 0,
                    x["explanation"]?.ToString() ?? ""
                )).ToList() ?? new List<(string, List<string>, int, string)>()
            );
        }
        catch
        {
            return ("Error", "Could not generate content", new List<(string, string, string, string)>(), new List<(string, List<string>, int, string)>());
        }
    }

    public async Task<List<(string word, string meaning, string pronunciation, string example)>> GenerateVocabularyAsync(string language, string theme, int count)
    {
        var langName = GetLanguageName(language);
        var prompt = $@"Generate {count} vocabulary words in {langName} related to the theme: {theme}.

Respond in JSON format only (no markdown):
{{
    ""words"": [
        {{""word"": ""<word in {langName}>"", ""meaning"": ""<Vietnamese meaning>"", ""pronunciation"": ""<romanization>"", ""example"": ""<example sentence in {langName} (Vietnamese translation)>""}}
    ]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            var json = ExtractJson(response);
            var root = JObject.Parse(json);
            return (root["words"] as JArray)?.Select(x => (
                x["word"]?.ToString() ?? "",
                x["meaning"]?.ToString() ?? "",
                x["pronunciation"]?.ToString() ?? "",
                x["example"]?.ToString() ?? ""
            )).ToList() ?? new List<(string, string, string, string)>();
        }
        catch
        {
            return new List<(string, string, string, string)>();
        }
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage)
    {
        var targetLangName = targetLanguage switch
        {
            "en" => "English",
            "zh" => "Chinese (Simplified)",
            "vi" => "Vietnamese",
            "ko" => "Korean",
            _ => "English"
        };

        var prompt = $@"Translate the following text to {targetLangName}. 
Return ONLY the translation, nothing else. No explanations, no quotes.

Text: {text}

Translation:";

        try
        {
            var translation = await CallGeminiAsync(prompt);
            return translation.Trim().Trim('"');
        }
        catch
        {
            return text; // Return original if translation fails
        }
    }

    private string GetLanguageName(string code) => LanguageNames.GetValueOrDefault(code, "English");

    private string BuildConversationPrompt(string system, List<(string role, string content)> history, string currentMessage)
    {
        var sb = new StringBuilder();
        sb.AppendLine(system);
        sb.AppendLine();
        foreach (var (role, content) in history)
        {
            sb.AppendLine($"{role}: {content}");
        }
        sb.AppendLine($"User: {currentMessage}");
        sb.AppendLine("Assistant:");
        return sb.ToString();
    }

    private string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text.Substring(start, end - start + 1);
        }
        return "{}";
    }
    public async Task<List<(string word, string meaning, string pronunciation, string example)>> EnrichVocabularyAsync(List<(string word, string meaning)> items)
    {
        if (items == null || items.Count == 0) return new List<(string, string, string, string)>();

        var itemsList = string.Join("\n", items.Select(x => $"{x.word}: {x.meaning}"));
        var prompt = $@"I have a list of vocabulary words. Some entries may contain synonyms separated by '/', '|', ',', ';', '\', or '='.
For such entries, please format the output as follows:
- Word: Keep the original input string (e.g. 'A / B')
- Pronunciation: Provide IPA for ALL synonyms, separated by ' - ' (e.g. '/ipa A/ - /ipa B/')
- Meaning: Format meanings in parentheses if possible or keep original.
- Example: Provide a short example sentence for EACH synonym in the target language, followed by its Vietnamese translation in parentheses. Format: 'Word A: Example sentence A (Vietnamese translation) / Word B: Example sentence B (Vietnamese translation)'.

Input:
{itemsList}

Respond in JSON format only (no markdown):
{{
    ""words"": [
        {{""word"": ""<original word>"", ""meaning"": ""<original meaning>"", ""pronunciation"": ""<IPA>"", ""example"": ""<example sentence in target language (Vietnamese translation)>""}}
    ]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            var json = ExtractJson(response);
            var root = JObject.Parse(json);
            return (root["words"] as JArray)?.Select(x => (
                x["word"]?.ToString() ?? "",
                x["meaning"]?.ToString() ?? "",
                x["pronunciation"]?.ToString() ?? "",
                x["example"]?.ToString() ?? ""
            )).ToList() ?? items.Select(x => (x.word, x.meaning, "", "")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching vocabulary");
            // Return original items with empty extra fields on error
            return items.Select(x => (x.word, x.meaning, "", "")).ToList();
        }
    }

    public async Task<string> TranscribeAudioAsync(byte[] audioData, string language, string mimeType = "audio/wav")
    {
        try
        {
            var langName = GetLanguageName(language);
            var base64Audio = Convert.ToBase64String(audioData);

            // Gemini request with audio input
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Audio
                                }
                            },
                            new
                            {
                                text = $"Please transcribe the audio. The speaker is saying a word or phrase in {langName}. " +
                                       "Return ONLY the transcribed text, nothing else. " +
                                       "If you cannot understand the audio clearly, return your best guess. " +
                                       "Do not add any explanation or punctuation, just the word(s)."
                            }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Use Gemini Pro for audio (Pro supports multimodal)
            var audioUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
            var response = await _httpClient.PostAsync($"{audioUrl}?key={_apiKey}", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var doc = JObject.Parse(responseText);
                var text = doc["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                return text?.Trim() ?? "";
            }

            _logger.LogError("Gemini transcribe error: {Response}", responseText);
            return "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing audio");
            return "";
        }
    }

    public async Task<LinguaAI.Common.Models.PracticeResponse> GeneratePracticeExercisesAsync(LinguaAI.Common.Models.PracticeRequest request)
    {
        var langName = GetLanguageName(request.Language);
        var typeDesc = request.Type switch
        {
            "fill_blank" => "Fill in the blank sentences",
            "arrange" => "Sentences with shuffled words",
            "translate" => "Sentences to translate",
            _ => "General practice sentences"
        };

        var prompt = $@"Generate {request.Count} language practice exercises for {request.Level} level students learning {langName}.
Type: {typeDesc} (Internal Type: {request.Type})
Vocabulary pool to integrate: {string.Join(", ", request.Words)}

LEVEL COMPLEXITY GUIDELINES:
- Elementary (Sơ cấp): Simple sentences, basic grammar, everyday situations (greeting, shopping, weather)
- Intermediate (Trung cấp): Compound sentences, varied grammar patterns, social contexts (work, travel, relationships)
- Advanced (Cao cấp): Complex sentences, formal/informal registers, abstract topics (opinions, culture, philosophy)

Requirements based on Type:
1. fill_blank: Provide a {langName} sentence with ONE vocabulary word replaced by '_____'. CorrectAnswer is the missing word. Options should include the correct word and 3 similar distractors.

2. arrange: 
   - Provide a complete {langName} sentence using the vocabulary.
   - The 'Options' list MUST contain ALL individual words from the sentence.
   - The words in 'Options' MUST be shuffled randomly.
   - CRITICAL: Ensure NO words are missing from the 'Options' list.
   - CorrectAnswer is the correctly ordered sentence.

3. translate: 
   - The question field MUST contain TWO parts:
     Part 1: Brief context in brackets (e.g., ""[Tại nhà hàng]"", ""[Nói chuyện với bạn]"")
     Part 2: A complete VIETNAMESE sentence to translate (REQUIRED! This is the sentence the user will translate)
   - Example question format: ""[Tại nhà hàng] Tôi muốn gọi một ly cà phê.""
   - The Vietnamese sentence naturally uses one of the vocabulary words (use its Vietnamese meaning in context)
   - DO NOT hint or reveal which vocabulary word is being used
   - CorrectAnswer is the {langName} translation
   - Sentence complexity must match the level (Elementary=simple, Intermediate=moderate, Advanced=complex)
   - 'Options' is empty
   - Explanation MUST include:
     * Word-by-word breakdown
     * Grammar structure explanation
     * Alternative acceptable translations

IMPORTANT: All explanations must be bilingual ({langName} + Vietnamese).

JSON Response format:
{{
    ""exercises"": [
        {{
            ""question"": ""<[Context] Vietnamese sentence to translate>"",
            ""correctAnswer"": ""<The correct answer>"",
            ""options"": [""<option1>"", ""<option2>"", ""... (MUST be populated for fill_blank and arrange)"", ""...""],
            ""explanation"": ""<Detailed bilingual explanation>"",
            ""explanationVi"": ""<Vietnamese explanation only>""
        }}
    ]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            _logger.LogInformation("Raw practice response length: {Length}", response.Length);
            
            var json = ExtractJson(response);
            _logger.LogInformation("Extracted JSON length: {Length}", json.Length);

             // Log the snippet around the error if possible, or just the JSON
            if (json.Length > 2000) 
            {
                 _logger.LogInformation("JSON Snippet (first 1000): {Json}", json.Substring(0, 1000));
                 _logger.LogInformation("JSON Snippet (last 200): {Json}", json.Substring(json.Length - 200));
            }
            else
            {
                _logger.LogInformation("Full JSON: {Json}", json);
            }
            
            // Basic cleanup for common issues
            json = json.Replace("\t", " ");

            // Use Newtonsoft.Json for more lenient parsing
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<LinguaAI.Common.Models.PracticeResponse>(json);
            return result ?? new LinguaAI.Common.Models.PracticeResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating practice exercises. Response length: {Length}", response.Length);
            // Try to log the specific part that might have failed if it's a JSON reader exception
            if (ex is Newtonsoft.Json.JsonReaderException jex) {
                 _logger.LogError("JSON Error at Line {Line}, Position {Pos}, Path {Path}", jex.LineNumber, jex.LinePosition, jex.Path);
            }
            return new LinguaAI.Common.Models.PracticeResponse();
        }
    }

    public async Task<LinguaAI.Common.Models.TranslationCheckResponse> CheckTranslationAsync(LinguaAI.Common.Models.TranslationCheckRequest request)
    {
        var langName = GetLanguageName(request.Language);
        var prompt = $@"You are a {langName} language teacher. Check the student's translation from Vietnamese to {langName}.

Original Vietnamese text: {request.OriginalText}
Student's translation: {request.UserAnswer}
Reference answer (for comparison): {request.ExpectedAnswer}

Evaluate the student's translation and provide detailed feedback.
Be lenient - accept translations that convey the same meaning even if they differ from the reference.
Consider synonyms, different sentence structures, and colloquial expressions as acceptable.

RESPOND IN PLAIN JSON FORMAT ONLY. Do not use Markdown code blocks.
{{
    ""isCorrect"": true/false,
    ""score"": <0-100>,
    ""feedback"": ""<Feedback in Vietnamese>"",
    ""correctedTranslation"": ""<The correct translation>"",
    ""wordByWordBreakdown"": ""<A SINGLE STRING describing the breakdown, NOT an object. Example: 'Word1: Meaning1, Word2: Meaning2'>"".
    ""grammarNotes"": ""<Grammar notes in Vietnamese>"",
    ""alternativeTranslations"": [""<alt1>"", ""<alt2>""]
}}";

        var response = await CallGeminiAsync(prompt);
        try
        {
            _logger.LogInformation("Raw check response: {Response}", response);
            var json = ExtractJson(response);
            
            // Clean up common JSON issues
            json = json.Replace("\t", " ");
            
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<LinguaAI.Common.Models.TranslationCheckResponse>(json);
            return result ?? new LinguaAI.Common.Models.TranslationCheckResponse { 
                Feedback = "Không thể phân tích câu trả lời",
                Score = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking translation. Response: {Response}", response);
            return new LinguaAI.Common.Models.TranslationCheckResponse { 
                Feedback = "Lỗi hệ thống: " + ex.Message,
                Score = 0
            };
        }
    }
}
