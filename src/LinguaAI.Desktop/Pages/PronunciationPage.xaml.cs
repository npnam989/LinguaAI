using System.Speech.Recognition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LinguaAI.Desktop.Services;
using NAudio.Wave;

namespace LinguaAI.Desktop.Pages;

public partial class PronunciationPage : Page
{
    private readonly LinguaApiService _apiService;
    private WaveInEvent? _waveIn;
    private SpeechRecognitionEngine? _recognizer;
    private bool _isRecording = false;
    private string _recognizedText = "";

    public PronunciationPage()
    {
        InitializeComponent();
        _apiService = new LinguaApiService();
        LoadNewPhrase();
    }

    private void LoadNewPhrase()
    {
        // Sample phrases for demo - in real app would come from API
        var phrases = new[]
        {
            ("안녕하세요", "annyeonghaseyo", "Xin chào"),
            ("감사합니다", "gamsahamnida", "Cảm ơn"),
            ("사랑해요", "saranghaeyo", "Yêu bạn"),
            ("만나서 반갑습니다", "mannaseo bangapseumnida", "Rất vui được gặp bạn"),
            ("좋은 하루 되세요", "joeun haru doeseyo", "Chúc một ngày tốt lành")
        };
        
        var random = new Random();
        var (text, romanization, meaning) = phrases[random.Next(phrases.Length)];
        
        PhraseText.Text = text;
        PhraseRomanization.Text = romanization;
        PhraseMeaning.Text = meaning;
        
        // Reset UI
        ScoreArea.Visibility = Visibility.Collapsed;
        SpokenArea.Visibility = Visibility.Collapsed;
        LoadingPanel.Visibility = Visibility.Collapsed;
        StatusText.Text = "Nhấn để ghi âm";
    }

    private void NewPhrase_Click(object sender, RoutedEventArgs e)
    {
        LoadNewPhrase();
    }

    private async void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isRecording)
        {
            StartRecording();
        }
        else
        {
            await StopRecordingAsync();
        }
    }

    private void StartRecording()
    {
        try
        {
            _recognizedText = "";
            StatusText.Text = "Đang ghi...";
            RecordButton.Background = (System.Windows.Media.Brush)FindResource("AccentPink");

            // Setup NAudio for Visualization
            _waveIn = new WaveInEvent();
            _waveIn.DeviceNumber = 0;
            _waveIn.WaveFormat = new WaveFormat(16000, 1);
            _waveIn.DataAvailable += OnAudioDataAvailable;
            _waveIn.StartRecording();

            // Setup System.Speech for Recognition
            if (_recognizer == null)
            {
                _recognizer = new SpeechRecognitionEngine();
                _recognizer.SetInputToDefaultAudioDevice();
                _recognizer.SpeechRecognized += (s, args) =>
                {
                    _recognizedText = args.Result.Text;
                };
            }

            _recognizer.UnloadAllGrammars();
            _recognizer.LoadGrammar(new DictationGrammar());
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);

            _isRecording = true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Lỗi bắt đầu ghi âm: {ex.Message}", "Lỗi");
        }
    }

    private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
    {
        // Calculate RMS for visualization
        double sum2 = 0;
        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
            sum2 += sample * sample;
        }
        double rms = Math.Sqrt(sum2 / (e.BytesRecorded / 2)) / 32768.0;

        Dispatcher.Invoke(() =>
        {
            AudioCanvas.Children.Clear();
            var height = AudioCanvas.ActualHeight > 0 ? AudioCanvas.ActualHeight : 60;
            var width = AudioCanvas.ActualWidth > 0 ? AudioCanvas.ActualWidth : 500;

            var barHeight = rms * height * 5;
            if (barHeight > height) barHeight = height;

            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = width,
                Height = barHeight,
                Fill = (System.Windows.Media.Brush)FindResource("GradientPrimary")
            };

            Canvas.SetTop(rect, (height - barHeight) / 2);
            AudioCanvas.Children.Add(rect);
        });
    }

    private async Task StopRecordingAsync()
    {
        _isRecording = false;
        StatusText.Text = "Đang phân tích...";
        RecordButton.Background = (System.Windows.Media.Brush)FindResource("GradientPrimary");
        LoadingPanel.Visibility = Visibility.Visible;

        // Stop Audio
        if (_waveIn != null)
        {
            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;
        }

        if (_recognizer != null)
        {
            _recognizer.RecognizeAsyncStop();
        }

        AudioCanvas.Children.Clear();
        await Task.Delay(1000);

        // Get spoken text
        var spoken = _recognizedText;
        if (string.IsNullOrEmpty(spoken))
        {
            spoken = PhraseText.Text; // Fallback for demo
        }

        // Show spoken text
        SpokenText.Text = spoken;
        SpokenArea.Visibility = Visibility.Visible;

        // Get language
        var language = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ko";

        // Call API
        var result = await _apiService.EvaluatePronunciationAsync(language, PhraseText.Text, spoken);
        
        LoadingPanel.Visibility = Visibility.Collapsed;

        if (result != null)
        {
            ScoreValue.Text = result.Score.ToString();
            
            // Determine result text
            if (result.Score >= 90)
                DetailedResult.Text = "Xuất sắc! 🎉";
            else if (result.Score >= 70)
                DetailedResult.Text = "Tốt lắm! 👍";
            else if (result.Score >= 50)
                DetailedResult.Text = "Khá ổn 👌";
            else
                DetailedResult.Text = "Cần luyện thêm 💪";

            FeedbackText.Text = result.Feedback;
            WordResults.ItemsSource = result.Words;
            ScoreArea.Visibility = Visibility.Visible;
            StatusText.Text = "Hoàn thành!";
        }
        else
        {
            System.Windows.MessageBox.Show("Lỗi kết nối API.", "Lỗi");
            StatusText.Text = "Lỗi";
        }
    }
}
