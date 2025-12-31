using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using LinguaAI.Common.Models;
using LinguaAI.Desktop.Services;

namespace LinguaAI.Desktop.Pages;

public partial class ConversationPage : Page
{
    private readonly LinguaApiService _apiService;
    private readonly List<ChatMessage> _history = new();
    private SpeechSynthesizer? _synthesizer;

    public ConversationPage()
    {
        InitializeComponent();
        _apiService = new LinguaApiService();
        _synthesizer = new SpeechSynthesizer();
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        _history.Clear();
        ChatPanel.Children.Clear();
        AddMessage("assistant", "Xin chào! Hãy bắt đầu cuộc hội thoại nhé! 👋");
    }

    private void MessageInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
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

        var language = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ko";
        var scenario = (ScenarioComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "coffee";

        try
        {
            var response = await _apiService.ChatAsync(language, scenario, message, _history);

            if (!string.IsNullOrEmpty(response))
            {
                AddMessage("assistant", response);
                _synthesizer?.SpeakAsync(response);
            }
            else
            {
                AddMessage("assistant", "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại.");
            }
        }
        catch (Exception ex)
        {
            AddMessage("assistant", $"Lỗi: {ex.Message}");
        }
    }

    private void AddMessage(string role, string content)
    {
        _history.Add(new ChatMessage { Role = role, Content = content });

        Border bubble;

        if (role == "user")
        {
            bubble = new Border
            {
                CornerRadius = new CornerRadius(12, 12, 4, 12),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(50, 5, 0, 5),
                MaxWidth = 400,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Background = (System.Windows.Media.Brush)FindResource("GradientPrimary")
            };
        }
        else
        {
            bubble = new Border
            {
                CornerRadius = new CornerRadius(12, 12, 12, 4),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(0, 5, 50, 5),
                MaxWidth = 400,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Background = (System.Windows.Media.Brush)FindResource("SurfaceBackground"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("Border"),
                BorderThickness = new Thickness(1)
            };
        }

        var text = new TextBlock
        {
            Text = content,
            Foreground = role == "user" ? System.Windows.Media.Brushes.White : (System.Windows.Media.Brush)FindResource("PrimaryText"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        };

        bubble.Child = text;
        ChatPanel.Children.Add(bubble);
        ChatScrollViewer.ScrollToBottom();
    }
}
