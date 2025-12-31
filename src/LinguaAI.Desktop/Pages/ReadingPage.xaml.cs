using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LinguaAI.Desktop.Services;

namespace LinguaAI.Desktop.Pages;

public partial class ReadingPage : Page
{
    private readonly LinguaApiService _apiService;
    private readonly DispatcherTimer _timer;
    private int _elapsedSeconds = 0;

    public ReadingPage()
    {
        InitializeComponent();
        _apiService = new LinguaApiService();
        
        // Setup timer
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _elapsedSeconds++;
        var minutes = _elapsedSeconds / 60;
        var seconds = _elapsedSeconds % 60;
        TimerText.Text = $"{minutes:D2}:{seconds:D2}";
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        var language = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ko";
        var level = (LevelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "intermediate";
        var topic = (TopicComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        
        if (string.IsNullOrEmpty(topic)) topic = null; // Random topic

        // Show loading
        EmptyState.Visibility = Visibility.Collapsed;
        ContentArea.Visibility = Visibility.Collapsed;
        LoadingPanel.Visibility = Visibility.Visible;
        GenerateButton.IsEnabled = false;

        try
        {
            var result = await _apiService.GenerateReadingAsync(language, level, topic);

            LoadingPanel.Visibility = Visibility.Collapsed;
            GenerateButton.IsEnabled = true;

            if (result != null)
            {
                TitleText.Text = result.Title;
                ContentText.Text = result.Content;
                VocabList.ItemsSource = result.Vocabulary;
                QuizList.ItemsSource = result.Questions;

                ContentArea.Visibility = Visibility.Visible;
                
                // Start timer
                _elapsedSeconds = 0;
                TimerText.Text = "00:00";
                _timer.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Không thể tạo bài đọc. Vui lòng thử lại.", "Lỗi");
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

    private void CheckAnswers_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        var minutes = _elapsedSeconds / 60;
        var seconds = _elapsedSeconds % 60;
        System.Windows.MessageBox.Show($"Thời gian hoàn thành: {minutes:D2}:{seconds:D2}", "Kết quả");
    }

    private void NextLesson_Click(object sender, RoutedEventArgs e)
    {
        GenerateButton_Click(sender, e);
    }
}
