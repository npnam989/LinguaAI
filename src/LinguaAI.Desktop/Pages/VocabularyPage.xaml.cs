using System.IO;
using System.Speech.Recognition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LinguaAI.Common.Models;
using LinguaAI.Desktop.Services;
using NAudio.Wave;
using Microsoft.Win32;

namespace LinguaAI.Desktop.Pages;

public partial class VocabularyPage : Page
{
    private readonly LinguaApiService _apiService;
    private List<VocabularyItem> _words = new();
    private int _currentIndex = 0;
    private bool _isFlipped = false;

    // Quiz state
    private int _quizIndex = 0;
    private int _correctCount = 0;
    private int _wrongCount = 0;
    private bool _isQuizRecording = false;
    private DispatcherTimer? _quizTimer;
    private int _quizTimeLeft = 10;
    private SpeechRecognitionEngine? _recognizer;
    private WaveInEvent? _waveIn;
    private string _recognizedText = "";

    // Noise gate / Voice Activity Detection
    private double _noiseThreshold = 15; // Default threshold (0-100 scale)
    private bool _isCalibrating = false;
    private List<double> _calibrationSamples = new();
    private DispatcherTimer? _calibrationTimer;
    private bool _isVoiceDetected = false;

    // AI Transcription
    private MemoryStream? _audioBuffer;
    private WaveFileWriter? _waveWriter;
    private bool _useAiTranscription = true; // Use Gemini AI by default

    public VocabularyPage()
    {
        InitializeComponent();
        _apiService = new LinguaApiService();
    }

    #region Generate / Upload

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedLang = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ko";
        var selectedTheme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "greetings";
        var count = int.Parse((CountComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "10");

        EmptyState.Visibility = Visibility.Collapsed;
        LoadingPanel.Visibility = Visibility.Visible;
        ModePanel.Visibility = Visibility.Collapsed;
        FlashcardArea.Visibility = Visibility.Collapsed;
        QuizArea.Visibility = Visibility.Collapsed;
        WordListArea.Visibility = Visibility.Collapsed;
        GenerateButton.IsEnabled = false;

        try
        {
            var result = await _apiService.GenerateVocabularyAsync(selectedLang, selectedTheme, count);

            LoadingPanel.Visibility = Visibility.Collapsed;
            GenerateButton.IsEnabled = true;

            if (result?.Words != null && result.Words.Count > 0)
            {
                _words = result.Words;
                _currentIndex = 0;
                _isFlipped = false;
                ModePanel.Visibility = Visibility.Visible;
                ShowFlashcardMode();
            }
            else
            {
                System.Windows.MessageBox.Show("Không thể tải từ vựng. Vui lòng thử lại.", "Lỗi");
                EmptyState.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            GenerateButton.IsEnabled = true;
            System.Windows.MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            EmptyState.Visibility = Visibility.Visible;
        }
    }

    private void DownloadExcelTemplate_Click(object sender, RoutedEventArgs e)
    {
        SaveTemplateToUserLocation("vocabulary_template.xlsx", "Excel Files|*.xlsx");
    }

    private void DownloadWordTemplate_Click(object sender, RoutedEventArgs e)
    {
        SaveTemplateToUserLocation("vocabulary_template.docx", "Word Files|*.docx");
    }

    private void SaveTemplateToUserLocation(string templateFileName, string filter)
    {
        try
        {
            // Get template from app directory
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var templatePath = Path.Combine(appDir, "Resources", templateFileName);

            if (!File.Exists(templatePath))
            {
                System.Windows.MessageBox.Show($"Không tìm thấy file mẫu: {templateFileName}", "Lỗi");
                return;
            }

            // Ask user where to save
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = templateFileName,
                Filter = filter,
                Title = "Lưu file mẫu"
            };

            if (saveDialog.ShowDialog() == true)
            {
                File.Copy(templatePath, saveDialog.FileName, overwrite: true);
                System.Windows.MessageBox.Show($"Đã lưu file mẫu: {saveDialog.FileName}", "Thành công");

                // Open the folder containing the file
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{saveDialog.FileName}\"");
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
        }
    }

    private void UploadButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Excel Files|*.xlsx|Word Files|*.docx",
            Title = "Chọn file từ vựng"
        };

        if (dialog.ShowDialog() == true)
        {
            FileNameText.Text = Path.GetFileName(dialog.FileName);
            LoadVocabularyFromFile(dialog.FileName);
        }
    }

    private void LoadVocabularyFromFile(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLower();
            var words = new List<VocabularyItem>();

            if (extension == ".xlsx")
            {
                words = ParseExcelFile(filePath);
            }
            else if (extension == ".docx")
            {
                words = ParseWordFile(filePath);
            }

            if (words.Count > 0)
            {
                _words = words;
                _currentIndex = 0;
                _isFlipped = false;
                EmptyState.Visibility = Visibility.Collapsed;
                ModePanel.Visibility = Visibility.Visible;
                ShowFlashcardMode();
                System.Windows.MessageBox.Show($"Đã tải {words.Count} từ từ file!", "Thành công");
            }
            else
            {
                System.Windows.MessageBox.Show("Không tìm thấy từ vựng trong file.", "Thông báo");
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Lỗi đọc file: {ex.Message}", "Lỗi");
        }
    }

    private List<VocabularyItem> ParseExcelFile(string filePath)
    {
        var words = new List<VocabularyItem>();

        try
        {
            // EPPlus 8+ requires new license API
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("LinguaAI");

            using var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null)
            {
                System.Windows.MessageBox.Show("File Excel không có worksheet.", "Lỗi");
                return words;
            }

            // Read from row 2 (skip header) to end
            int rowCount = worksheet.Dimension?.Rows ?? 0;
            
            for (int row = 2; row <= rowCount; row++)
            {
                var word = worksheet.Cells[row, 1].Text?.Trim();
                var meaning = worksheet.Cells[row, 2].Text?.Trim();
                
                if (!string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(meaning))
                {
                    words.Add(new VocabularyItem
                    {
                        Word = word,
                        Meaning = meaning,
                        Pronunciation = worksheet.Cells[row, 3].Text?.Trim() ?? "",
                        Example = worksheet.Cells[row, 4].Text?.Trim() ?? ""
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Lỗi đọc file Excel: {ex.Message}", "Lỗi");
        }

        return words;
    }

    private List<VocabularyItem> ParseWordFile(string filePath)
    {
        var words = new List<VocabularyItem>();

        try
        {
            // Read Word file as text (simplified approach)
            // Each line format: Word - Meaning
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(new[] { " - ", " – ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    words.Add(new VocabularyItem
                    {
                        Word = parts[0].Trim(),
                        Meaning = parts[1].Trim(),
                        Pronunciation = parts.Length > 2 ? parts[2].Trim() : "",
                        Example = parts.Length > 3 ? parts[3].Trim() : ""
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Lỗi đọc file Word. Thử chuyển sang định dạng text (.txt).\n{ex.Message}", "Lỗi");
        }

        return words;
    }

    #endregion

    #region Mode Switching

    private void FlashcardMode_Click(object sender, RoutedEventArgs e) => ShowFlashcardMode();
    private void QuizMode_Click(object sender, RoutedEventArgs e) => ShowQuizMode();
    private void ListMode_Click(object sender, RoutedEventArgs e) => ShowListMode();

    private void ShowFlashcardMode()
    {
        FlashcardModeBtn.Style = (Style)FindResource("PrimaryButton");
        QuizModeBtn.Style = (Style)FindResource("SecondaryButton");
        ListModeBtn.Style = (Style)FindResource("SecondaryButton");

        FlashcardArea.Visibility = Visibility.Visible;
        QuizArea.Visibility = Visibility.Collapsed;
        WordListArea.Visibility = Visibility.Collapsed;

        UpdateFlashcard();
    }

    private void ShowQuizMode()
    {
        QuizModeBtn.Style = (Style)FindResource("PrimaryButton");
        FlashcardModeBtn.Style = (Style)FindResource("SecondaryButton");
        ListModeBtn.Style = (Style)FindResource("SecondaryButton");

        FlashcardArea.Visibility = Visibility.Collapsed;
        QuizArea.Visibility = Visibility.Visible;
        WordListArea.Visibility = Visibility.Collapsed;

        StartQuiz();
    }

    private void ShowListMode()
    {
        ListModeBtn.Style = (Style)FindResource("PrimaryButton");
        FlashcardModeBtn.Style = (Style)FindResource("SecondaryButton");
        QuizModeBtn.Style = (Style)FindResource("SecondaryButton");

        FlashcardArea.Visibility = Visibility.Collapsed;
        QuizArea.Visibility = Visibility.Collapsed;
        WordListArea.Visibility = Visibility.Visible;

        VocabularyList.ItemsSource = _words;
        WordCountText.Text = _words.Count.ToString();
    }

    #endregion

    #region Flashcard Mode

    private void UpdateFlashcard()
    {
        if (_words.Count == 0) return;

        var word = _words[_currentIndex];
        CurrentIndexText.Text = (_currentIndex + 1).ToString();
        TotalCountText.Text = _words.Count.ToString();

        _isFlipped = false;
        ShowCardFront(word);
    }

    private void ShowCardFront(VocabularyItem word)
    {
        Flashcard.Style = (Style)FindResource("FlashcardFront");
        CardWord.Text = word.Word ?? "";
        CardWord.Foreground = System.Windows.Media.Brushes.White;
        CardPronunciation.Text = word.Pronunciation ?? "";
        CardPronunciation.Visibility = Visibility.Visible;
        CardMeaning.Visibility = Visibility.Collapsed;
        CardExample.Visibility = Visibility.Collapsed;
    }

    private void ShowCardBack(VocabularyItem word)
    {
        Flashcard.Style = (Style)FindResource("FlashcardBack");
        CardWord.Text = word.Word ?? "";
        CardWord.Foreground = (System.Windows.Media.Brush)FindResource("PrimaryText");
        CardPronunciation.Visibility = Visibility.Collapsed;
        CardMeaning.Text = word.Meaning ?? "";
        CardMeaning.Visibility = Visibility.Visible;
        CardExample.Text = word.Example ?? "";
        CardExample.Visibility = !string.IsNullOrEmpty(word.Example) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Flashcard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_words.Count == 0) return;

        _isFlipped = !_isFlipped;
        var word = _words[_currentIndex];

        if (_isFlipped)
            ShowCardBack(word);
        else
            ShowCardFront(word);
    }

    private void PrevCard_Click(object sender, RoutedEventArgs e)
    {
        if (_words.Count == 0) return;
        _currentIndex = (_currentIndex - 1 + _words.Count) % _words.Count;
        UpdateFlashcard();
    }

    private void NextCard_Click(object sender, RoutedEventArgs e)
    {
        if (_words.Count == 0) return;
        _currentIndex = (_currentIndex + 1) % _words.Count;
        UpdateFlashcard();
    }

    private void ShuffleCards_Click(object sender, RoutedEventArgs e)
    {
        if (_words.Count == 0) return;
        var random = new Random();
        _words = _words.OrderBy(_ => random.Next()).ToList();
        _currentIndex = 0;
        UpdateFlashcard();
    }

    #endregion

    #region Quiz Mode

    private const int QUIZ_TIME_LIMIT = 15; // Match Web version: 15 seconds

    private void StartQuiz()
    {
        _quizIndex = 0;
        _correctCount = 0;
        _wrongCount = 0;

        // Shuffle words for quiz
        var random = new Random();
        _words = _words.OrderBy(_ => random.Next()).ToList();

        QuizScoreSummary.Visibility = Visibility.Collapsed;
        ShowQuizQuestion();
    }

    private void ShowQuizQuestion()
    {
        // Stop any ongoing recording
        if (_isQuizRecording)
        {
            StopQuizRecordingQuiet();
        }

        // Stop previous timer
        _quizTimer?.Stop();

        if (_quizIndex >= _words.Count)
        {
            ShowQuizSummary();
            return;
        }

        var word = _words[_quizIndex];
        QuizCurrentIndex.Text = (_quizIndex + 1).ToString();
        QuizTotalCount.Text = _words.Count.ToString();
        QuizMeaning.Text = word.Meaning ?? "";
        
        // Better hint: show pronunciation like Web version
        QuizHint.Text = $"Gợi ý: {word.Pronunciation ?? "..."}";
        QuizHint.Visibility = Visibility.Collapsed;

        // Reset UI
        QuizAnswerArea.Visibility = Visibility.Collapsed;
        QuizResultArea.Visibility = Visibility.Collapsed;
        QuizRecordBtn.IsEnabled = true;
        QuizRecordStatus.Text = "Nhấn để trả lời";
        VolumeMeter.Width = 0;

        // Reset timer (15 seconds like Web)
        _quizTimeLeft = QUIZ_TIME_LIMIT;
        QuizTimerText.Text = QUIZ_TIME_LIMIT.ToString();
        QuizTimerText.Foreground = (System.Windows.Media.Brush)FindResource("AccentGold");
        QuizTimerBar.Width = 200;

        StartQuizTimer();
    }

    private void StartQuizTimer()
    {
        _quizTimer?.Stop();
        _quizTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _quizTimer.Tick += (s, e) =>
        {
            _quizTimeLeft--;
            QuizTimerText.Text = _quizTimeLeft.ToString();
            QuizTimerBar.Width = 200.0 * _quizTimeLeft / QUIZ_TIME_LIMIT;

            // Change color when time is running out (like Web version)
            if (_quizTimeLeft <= 3)
            {
                QuizTimerText.Foreground = (System.Windows.Media.Brush)FindResource("AccentPink");
            }

            if (_quizTimeLeft <= 0)
            {
                _quizTimer?.Stop();
                TimeUp();
            }
        };
        _quizTimer.Start();
    }

    private void TimeUp()
    {
        if (_isQuizRecording)
        {
            StopQuizRecordingQuiet();
        }
        
        _wrongCount++;
        
        var word = _words[_quizIndex];
        QuizResultIcon.Text = "⏰";
        QuizResultIcon.Foreground = (System.Windows.Media.Brush)FindResource("AccentPink");
        QuizResultText.Text = "Hết giờ!";
        QuizResultText.Foreground = (System.Windows.Media.Brush)FindResource("AccentPink");
        QuizCorrectAnswer.Text = $"Đáp án: {word.Word} ({word.Pronunciation ?? ""})";
        QuizResultArea.Visibility = Visibility.Visible;
        QuizRecordBtn.IsEnabled = false;
    }

    private void QuizRecord_Click(object sender, RoutedEventArgs e)
    {
        if (!_isQuizRecording)
        {
            StartQuizRecording();
        }
        else
        {
            StopQuizRecording();
        }
    }

    private void StartQuizRecording()
    {
        try
        {
            _recognizedText = "";
            _isQuizRecording = true;
            QuizRecordStatus.Text = "Đang nghe...";
            QuizRecordBtn.Background = (System.Windows.Media.Brush)FindResource("AccentPink");

            // Setup audio (16kHz for better API compatibility)
            _waveIn = new WaveInEvent { DeviceNumber = 0 };
            _waveIn.WaveFormat = new WaveFormat(16000, 16, 1); // 16kHz 16-bit mono for Gemini
            _waveIn.BufferMilliseconds = 50;
            _waveIn.DataAvailable += OnQuizAudioData;

            // Setup audio buffer for AI transcription
            if (_useAiTranscription)
            {
                _audioBuffer = new MemoryStream();
                _waveWriter = new WaveFileWriter(_audioBuffer, _waveIn.WaveFormat);
            }

            _waveIn.StartRecording();

            // Get selected language
            var selectedLang = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ko";
            
            // Also setup Windows speech recognition as fallback
            if (!_useAiTranscription)
            {
                SetupSpeechRecognizer(selectedLang);
                _recognizer!.SpeechRecognized += OnSpeechRecognized;
                _recognizer.SpeechHypothesized += OnSpeechHypothesized;
                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Lỗi microphone: {ex.Message}", "Lỗi");
            _isQuizRecording = false;
        }
    }

    private void SetupSpeechRecognizer(string language)
    {
        // Map language code to culture
        var cultureMap = new Dictionary<string, string>
        {
            { "ko", "ko-KR" },
            { "zh", "zh-CN" },
            { "en", "en-US" }
        };
        
        var cultureName = cultureMap.GetValueOrDefault(language, "en-US");
        var culture = new System.Globalization.CultureInfo(cultureName);

        try
        {
            // Dispose old recognizer if culture changed
            if (_recognizer != null)
            {
                try { _recognizer.RecognizeAsyncStop(); } catch { }
                _recognizer.Dispose();
                _recognizer = null;
            }

            // Try to create recognizer with specific language
            _recognizer = new SpeechRecognitionEngine(culture);
        }
        catch
        {
            // Fallback to default recognizer if language not installed
            _recognizer = new SpeechRecognitionEngine();
        }

        _recognizer.SetInputToDefaultAudioDevice();

        // Build vocabulary-specific grammar for better recognition
        var vocabGrammar = BuildVocabularyGrammar();
        if (vocabGrammar != null)
        {
            _recognizer.LoadGrammar(vocabGrammar);
        }

        // Also load dictation grammar as fallback with lower weight
        var dictation = new DictationGrammar();
        dictation.Weight = 0.3f;
        _recognizer.LoadGrammar(dictation);

        // Improve recognition settings
        _recognizer.BabbleTimeout = TimeSpan.FromSeconds(0);
        _recognizer.InitialSilenceTimeout = TimeSpan.FromSeconds(10);
        _recognizer.EndSilenceTimeout = TimeSpan.FromMilliseconds(500);
        _recognizer.EndSilenceTimeoutAmbiguous = TimeSpan.FromMilliseconds(300);
    }

    private Grammar? BuildVocabularyGrammar()
    {
        if (_words.Count == 0) return null;

        try
        {
            // Create grammar with all vocabulary words
            var choices = new Choices();
            foreach (var word in _words)
            {
                if (!string.IsNullOrEmpty(word.Word))
                {
                    choices.Add(word.Word);
                    
                    // Also add pronunciation if different
                    if (!string.IsNullOrEmpty(word.Pronunciation) && word.Pronunciation != word.Word)
                    {
                        choices.Add(word.Pronunciation);
                    }
                }
            }

            var builder = new GrammarBuilder(choices);
            var grammar = new Grammar(builder);
            grammar.Weight = 1.0f; // High priority for vocabulary words
            grammar.Name = "VocabularyGrammar";
            return grammar;
        }
        catch
        {
            return null;
        }
    }

    private void OnSpeechHypothesized(object? sender, SpeechHypothesizedEventArgs e)
    {
        // Real-time feedback (interim results) like Web version
        Dispatcher.Invoke(() =>
        {
            QuizRecordStatus.Text = $"\"{e.Result.Text}\"";
        });
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        _recognizedText = e.Result.Text;
        Dispatcher.Invoke(() =>
        {
            QuizRecordStatus.Text = $"\"{e.Result.Text}\"";
        });
    }

    private void OnQuizAudioData(object? sender, WaveInEventArgs e)
    {
        // Write to buffer for AI transcription
        if (_useAiTranscription && _waveWriter != null)
        {
            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
        }

        double sum2 = 0;
        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
            sum2 += sample * sample;
        }
        double rms = Math.Sqrt(sum2 / (e.BytesRecorded / 2)) / 32768.0;
        double volumePercent = Math.Min(rms * 500, 100);

        // Voice Activity Detection: check if volume is above noise threshold
        _isVoiceDetected = volumePercent > _noiseThreshold;

        // If calibrating, collect samples
        if (_isCalibrating)
        {
            _calibrationSamples.Add(volumePercent);
        }

        Dispatcher.Invoke(() =>
        {
            VolumeMeter.Width = 150 * volumePercent / 100;
            
            // Change color based on voice detection
            if (_isVoiceDetected)
            {
                VolumeMeter.Background = (System.Windows.Media.Brush)FindResource("Primary");
            }
            else
            {
                VolumeMeter.Background = (System.Windows.Media.Brush)FindResource("SecondaryText");
            }
        });
    }

    private async void CalibrateNoise_Click(object sender, RoutedEventArgs e)
    {
        if (_isCalibrating) return;

        CalibrateBtn.IsEnabled = false;
        CalibrateBtn.Content = "🔄 Đang hiệu chỉnh...";
        NoiseThresholdText.Text = "";

        _isCalibrating = true;
        _calibrationSamples.Clear();

        // Start audio capture for calibration
        try
        {
            _waveIn = new WaveInEvent { DeviceNumber = 0 };
            _waveIn.WaveFormat = new WaveFormat(16000, 1);
            _waveIn.DataAvailable += OnQuizAudioData;
            _waveIn.StartRecording();

            // Calibrate for 2 seconds
            await Task.Delay(2000);

            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;

            _isCalibrating = false;

            // Calculate threshold: average noise + buffer
            if (_calibrationSamples.Count > 0)
            {
                double avgNoise = _calibrationSamples.Average();
                _noiseThreshold = Math.Round(avgNoise * 1.5 + 5);
                _noiseThreshold = Math.Max(5, Math.Min(80, _noiseThreshold)); // Clamp 5-80

                NoiseThresholdText.Text = $"Ngưỡng: {_noiseThreshold:F0}";
                CalibrateBtn.Content = $"✅ Đã hiệu chỉnh";
            }
            else
            {
                CalibrateBtn.Content = "❌ Lỗi";
            }
        }
        catch (Exception ex)
        {
            _isCalibrating = false;
            CalibrateBtn.Content = "❌ Lỗi";
            NoiseThresholdText.Text = ex.Message;
        }

        // Reset button after 2 seconds
        await Task.Delay(2000);
        CalibrateBtn.Content = "🔇 Hiệu chỉnh tiếng ồn";
        CalibrateBtn.IsEnabled = true;
    }

    private void StopQuizRecordingQuiet()
    {
        // Stop without evaluating (for cleanup)
        _isQuizRecording = false;
        
        if (_recognizer != null)
        {
            _recognizer.SpeechRecognized -= OnSpeechRecognized;
            _recognizer.SpeechHypothesized -= OnSpeechHypothesized;
            try { _recognizer.RecognizeAsyncStop(); } catch { }
        }

        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        _waveIn = null;
        VolumeMeter.Width = 0;
    }

    private async void StopQuizRecording()
    {
        _isQuizRecording = false;
        QuizRecordBtn.Background = (System.Windows.Media.Brush)FindResource("GradientPrimary");
        QuizRecordStatus.Text = "Đang xử lý bằng AI...";

        // Stop audio
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        _waveIn = null;
        _quizTimer?.Stop();
        VolumeMeter.Width = 0;

        if (_useAiTranscription && _waveWriter != null && _audioBuffer != null)
        {
            try
            {
                // Finalize WAV file
                _waveWriter.Flush();
                var audioData = _audioBuffer.ToArray();
                _waveWriter.Dispose();
                _waveWriter = null;
                _audioBuffer.Dispose();
                _audioBuffer = null;

                if (audioData.Length > 100) // Minimum valid audio
                {
                    // Get language
                    var selectedLang = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ko";

                    // Call API for AI transcription
                    var transcript = await _apiService.TranscribeAudioAsync(audioData, selectedLang);
                    _recognizedText = transcript ?? "";

                    Dispatcher.Invoke(() =>
                    {
                        QuizRecordStatus.Text = string.IsNullOrEmpty(_recognizedText) 
                            ? "(Không nhận diện được)" 
                            : $"\"{_recognizedText}\"";
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    QuizRecordStatus.Text = $"Lỗi AI: {ex.Message}";
                });
            }
        }
        else
        {
            // Fallback: Stop Windows speech recognition
            if (_recognizer != null)
            {
                _recognizer.SpeechRecognized -= OnSpeechRecognized;
                _recognizer.SpeechHypothesized -= OnSpeechHypothesized;
                try { _recognizer.RecognizeAsyncStop(); } catch { }
            }
            await Task.Delay(300); // Wait for recognition
        }

        // Evaluate answer
        EvaluateQuizAnswer();
    }

    private void EvaluateQuizAnswer()
    {
        var spoken = _recognizedText.Trim();
        var word = _words[_quizIndex];
        var expected = word.Word ?? "";

        // Show user's answer
        QuizUserAnswer.Text = string.IsNullOrEmpty(spoken) ? "(Không nhận diện được)" : spoken;
        QuizAnswerArea.Visibility = Visibility.Visible;

        // Fuzzy matching like Web version (Levenshtein distance)
        bool isCorrect = IsCorrectAnswer(spoken, expected);

        if (isCorrect)
        {
            _correctCount++;
            QuizResultIcon.Text = "✅";
            QuizResultIcon.Foreground = (System.Windows.Media.Brush)FindResource("AccentGreen");
            QuizResultText.Text = "Chính xác!";
            QuizResultText.Foreground = (System.Windows.Media.Brush)FindResource("AccentGreen");
            QuizCorrectAnswer.Text = "";
        }
        else
        {
            _wrongCount++;
            QuizResultIcon.Text = "❌";
            QuizResultIcon.Foreground = (System.Windows.Media.Brush)FindResource("Error");
            QuizResultText.Text = "Chưa đúng!";
            QuizResultText.Foreground = (System.Windows.Media.Brush)FindResource("Error");
            QuizCorrectAnswer.Text = $"Đáp án: {word.Word} ({word.Pronunciation ?? ""})";
        }

        QuizResultArea.Visibility = Visibility.Visible;
        QuizRecordBtn.IsEnabled = false;
    }

    private bool IsCorrectAnswer(string spoken, string expected)
    {
        if (string.IsNullOrEmpty(spoken)) return false;

        var userWord = spoken.ToLower().Trim();
        var correctWord = expected.ToLower().Trim();

        // Exact match
        if (userWord == correctWord) return true;

        // Contains match
        if (correctWord.Contains(userWord) || userWord.Contains(correctWord)) return true;

        // Levenshtein distance <= 2 (fuzzy match like Web version)
        if (LevenshteinDistance(userWord, correctWord) <= 2) return true;

        return false;
    }

    // Levenshtein distance for fuzzy matching (same algorithm as Web version)
    private int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var matrix = new int[b.Length + 1, a.Length + 1];

        for (int i = 0; i <= b.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= a.Length; j++) matrix[0, j] = j;

        for (int i = 1; i <= b.Length; i++)
        {
            for (int j = 1; j <= a.Length; j++)
            {
                if (b[i - 1] == a[j - 1])
                {
                    matrix[i, j] = matrix[i - 1, j - 1];
                }
                else
                {
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j - 1] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j] + 1
                    );
                }
            }
        }
        return matrix[b.Length, a.Length];
    }

    private void ShowQuizSummary()
    {
        _quizTimer?.Stop();
        QuizScoreSummary.Visibility = Visibility.Visible;
        CorrectCount.Text = _correctCount.ToString();
        WrongCount.Text = _wrongCount.ToString();
        
        // Hide other areas
        QuizResultArea.Visibility = Visibility.Collapsed;
        QuizAnswerArea.Visibility = Visibility.Collapsed;
    }

    private void QuizNext_Click(object sender, RoutedEventArgs e)
    {
        _quizIndex++;
        ShowQuizQuestion();
    }

    private void QuizHint_Click(object sender, RoutedEventArgs e)
    {
        QuizHint.Visibility = Visibility.Visible;
    }

    private void QuizSkip_Click(object sender, RoutedEventArgs e)
    {
        _quizTimer?.Stop();
        if (_isQuizRecording) StopQuizRecordingQuiet();
        _wrongCount++;
        _quizIndex++;
        ShowQuizQuestion();
    }

    private void QuizRestart_Click(object sender, RoutedEventArgs e)
    {
        StartQuiz();
    }

    #endregion
}
