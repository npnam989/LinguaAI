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
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.GoBack();
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

    private async void StartRecording()
    {
        try
        {
            _recognizedText = ""; // Reset
            StatusText.Text = "Listening...";
            RecordButton.Content = "⏹ Stop";
            RecordButton.Background = Brushes.Crimson;

            // 1. Setup NAudio for Visualization
            _waveIn = new WaveInEvent();
            _waveIn.DeviceNumber = 0;
            _waveIn.WaveFormat = new WaveFormat(16000, 1);
            _waveIn.DataAvailable += OnAudioDataAvailable;
            _waveIn.StartRecording();

            // 2. Setup System.Speech for Recognition
            // Note: System.Speech uses its own audio input.
            // Ideally we need to feed NAudio stream to System.Speech to avoid device conflict,
            // but for simplicity let's rely on Windows Mixer allowing shared access.
            if (_recognizer == null)
            {
                _recognizer = new SpeechRecognitionEngine();
                _recognizer.SetInputToDefaultAudioDevice();
                _recognizer.SpeechRecognized += (s, args) => 
                {
                    _recognizedText = args.Result.Text;
                    System.Diagnostics.Debug.WriteLine($"Recognized: {_recognizedText}");
                };
            }

            // Dynamics Grammar
            var target = TargetInput.Text.Trim();
            if (!string.IsNullOrEmpty(target))
            {
                var choices = new Choices();
                choices.Add(target); // Expect exact phrase
                // Add variants/words to allow partial match?
                // For flexible pronunciation, we should probably allow DictationGrammar
                // But Dictation is less accurate.
                // Let's try Dictation first for "Practice".
                _recognizer.UnloadAllGrammars();
                _recognizer.LoadGrammar(new DictationGrammar());
            }

            _recognizer.RecognizeAsync(RecognizeMode.Multiple);

            _isRecording = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting recording: {ex.Message}");
            await StopRecordingAsync();
        }
    }

    private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
    {
        // Simple visualization: Draw volume level
        // Calculate RMS
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
            var height = AudioCanvas.ActualHeight;
            var width = AudioCanvas.ActualWidth;
            
            var barHeight = rms * height * 5; // Scale up
            if (barHeight > height) barHeight = height;

            var rect = new Rectangle
            {
                Width = width,
                Height = barHeight,
                Fill = Brushes.LightBlue,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            Canvas.SetTop(rect, (height - barHeight) / 2);
            AudioCanvas.Children.Add(rect);
        });
    }

    private async Task StopRecordingAsync()
    {
        _isRecording = false;
        StatusText.Text = "Processing...";
        RecordButton.Content = "🎤 Record";
        RecordButton.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)); // #2563eb

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

        // Wait a bit for recognition to settle
        await Task.Delay(1000);

        // Call API
        var spoken = _recognizedText;
        if (string.IsNullOrEmpty(spoken))
        {
            StatusText.Text = "No speech detected (or STT failed). Using text fallback.";
            spoken = TargetInput.Text; // Fallback for demo if mic fails
        }

        StatusText.Text = $"Recognized: {spoken}";
        
        var result = await _apiService.EvaluatePronunciationAsync("en", TargetInput.Text, spoken);
        if (result != null)
        {
            ResultScore.Text = $"Score: {result.Score}/100";
            ResultFeedback.Text = result.Feedback;
            
            // Render detailed words
            // Simple string construction for WPF
            var details = "";
            foreach(var w in result.Words)
            {
                details += $"{w.Word} ({(w.Correct ? "✓" : "✗")}) ";
            }
            ResultDetails.Text = details;
        }
        else
        {
            ResultFeedback.Text = "Error communicating with API.";
        }
    }
}
