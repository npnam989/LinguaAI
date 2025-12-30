using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LinguaAI.Common.Models;
using LinguaAI.Desktop.Services;

namespace LinguaAI.Desktop.Pages;

public partial class ConversationPage : Page
{
    private readonly LinguaApiService _apiService;
    private readonly List<ChatMessage> _history = new();
    private SpeechRecognitionEngine? _recognizer;
    private SpeechSynthesizer? _synthesizer;
    private bool _isRecording = false;

    public ConversationPage()
    {
        InitializeComponent();
        _apiService = new LinguaApiService();
        _synthesizer = new SpeechSynthesizer();
        // Add initial bot message
        AddMessage("assistant", "Hello! How can I help you practice today?");
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.GoBack();
    }

    private void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendButton_Click(sender, e);
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var message = MessageInput.Text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        MessageInput.Text = "";
        AddMessage("user", message);

        var scenario = (ScenarioComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "General Chat";
        
        // Show typing indicator?
        // For now just wait
        var response = await _apiService.ChatAsync("en", scenario, message, _history);
        
        AddMessage("assistant", response);
        
        // Speak response
        _synthesizer?.SpeakAsync(response);
    }

    private void AddMessage(string role, string content)
    {
        _history.Add(new ChatMessage { Role = role, Content = content });

        var bubble = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Margin = new Thickness(5),
            MaxWidth = 600,
            HorizontalAlignment = role == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Background = role == "user" ? new SolidColorBrush(Color.FromRgb(37, 99, 235)) : new SolidColorBrush(Color.FromRgb(243, 244, 246))
        };

        var text = new TextBlock
        {
            Text = content,
            Foreground = role == "user" ? Brushes.White : Brushes.Black,
            TextWrapping = TextWrapping.Wrap
        };

        bubble.Child = text;
        ChatPanel.Children.Add(bubble);
        ChatScrollViewer.ScrollToBottom();
    }

    private void MicButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    private void StartRecording()
    {
        try 
        {
            if (_recognizer == null)
            {
                _recognizer = new SpeechRecognitionEngine();
                _recognizer.SetInputToDefaultAudioDevice();
                _recognizer.LoadGrammar(new DictationGrammar());
                _recognizer.SpeechRecognized += (s, args) => 
                {
                    MessageInput.Text = args.Result.Text;
                };
            }

            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            _isRecording = true;
            MicButton.Background = Brushes.Crimson;
            MicButton.Foreground = Brushes.White;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting mic: {ex.Message}");
        }
    }

    private void StopRecording()
    {
        if (_recognizer != null)
        {
            _recognizer.RecognizeAsyncStop();
        }
        _isRecording = false;
        MicButton.Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)); // #f3f4f6
        MicButton.Foreground = Brushes.Black;
    }
}
