using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LinguaAI.Common.Models;
using LinguaAI.Desktop.Services;

namespace LinguaAI.Desktop.Pages;

public partial class ReadingPage : Page
{
    private readonly LinguaApiService _apiService;
    private readonly DispatcherTimer _timer;
    private int _elapsedSeconds = 0;
    
    // Quiz state
    private ReadingResponse? _currentReading;
    private Dictionary<int, int> _userAnswers = new(); // questionIndex -> selectedOptionIndex

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
        
        if (string.IsNullOrEmpty(topic)) topic = null;

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
                _currentReading = result;
                _userAnswers.Clear();

                TitleText.Text = result.Title;
                ContentText.Text = result.Content;
                VocabList.ItemsSource = result.Vocabulary;

                // Build quiz UI with tracking
                BuildQuizUI(result.Questions);

                ContentArea.Visibility = Visibility.Visible;
                QuizResultPanel.Visibility = Visibility.Collapsed;
                CheckAnswersBtn.Visibility = Visibility.Visible;
                NextLessonBtn.Visibility = Visibility.Collapsed;
                
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

    private void BuildQuizUI(List<QuizQuestion> questions)
    {
        QuizList.Children.Clear();

        for (int qIdx = 0; qIdx < questions.Count; qIdx++)
        {
            var q = questions[qIdx];
            var questionIndex = qIdx;

            // Question container
            var mainBorder = new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("SurfaceBackground"),
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var stack = new StackPanel();

            // Question text
            var questionText = new TextBlock
            {
                Text = $"{qIdx + 1}. {q.Question}",
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)FindResource("PrimaryText"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(questionText);

            // Options
            var groupName = $"Q{qIdx}";
            for (int oIdx = 0; oIdx < q.Options.Count; oIdx++)
            {
                var optionIndex = oIdx;
                
                // Wrap RadioButton in a Border for highlighting
                var optionBorder = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 2, 0, 2),
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderBrush = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(1)
                };

                var radio = new System.Windows.Controls.RadioButton
                {
                    Content = q.Options[oIdx],
                    Foreground = (System.Windows.Media.Brush)FindResource("SecondaryText"),
                    GroupName = groupName,
                    Tag = $"{questionIndex}_{optionIndex}",
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                radio.Checked += (s, e) =>
                {
                    _userAnswers[questionIndex] = optionIndex;
                };

                // Make the whole border clickable
                optionBorder.MouseLeftButtonUp += (s, e) => radio.IsChecked = true;
                
                optionBorder.Child = radio;
                stack.Children.Add(optionBorder);
            }

            // Explanation (Hidden by default)
            if (!string.IsNullOrEmpty(q.Explanation))
            {
                var explanationText = new TextBlock
                {
                    Text = $"💡 Giải thích: {q.Explanation}",
                    Foreground = (System.Windows.Media.Brush)FindResource("AccentGreen"), // Using green for explanation
                    FontSize = 13,
                    FontStyle = FontStyles.Italic,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 10, 0, 0),
                    Visibility = Visibility.Collapsed,
                    Tag = "Explanation"
                };
                stack.Children.Add(explanationText);
            }

            mainBorder.Child = stack;
            QuizList.Children.Add(mainBorder);
        }
    }

    private void CheckAnswers_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();

        if (_currentReading == null || _currentReading.Questions.Count == 0)
        {
            System.Windows.MessageBox.Show("Không có câu hỏi để kiểm tra!", "Thông báo");
            return;
        }

        int correct = 0;
        var questions = _currentReading.Questions;

        // Iterate through quiz items and highlight
        for (int i = 0; i < QuizList.Children.Count && i < questions.Count; i++)
        {
            var mainBorder = QuizList.Children[i] as Border;
            if (mainBorder?.Child is not StackPanel stack) continue;

            var q = questions[i];
            int optionIdx = 0;

            foreach (var child in stack.Children)
            {
                // Show Explanation
                if (child is TextBlock tb && tb.Tag?.ToString() == "Explanation")
                {
                    tb.Visibility = Visibility.Visible;
                    continue;
                }

                // Skip the question TextBlock, look for Option Borders
                if (child is Border optionBorder && optionBorder.Child is System.Windows.Controls.RadioButton radio)
                {
                    // Reset style
                    optionBorder.Background = System.Windows.Media.Brushes.Transparent;
                    optionBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;

                    bool isCorrectAnswer = (optionIdx == q.CorrectIndex);
                    bool isSelected = _userAnswers.TryGetValue(i, out int selected) && selected == optionIdx;

                    if (isCorrectAnswer)
                    {
                        // Correct answer - highlight green
                        optionBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 0, 255, 128));
                        optionBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("AccentGreen");
                    }
                    else if (isSelected) // Selected but wrong (since we checked isCorrectAnswer above)
                    {
                        // Wrong answer - highlight pink
                        optionBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 64, 129));
                        optionBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("AccentPink");
                    }

                    radio.IsEnabled = false; // Disable after check
                    optionBorder.MouseLeftButtonUp -= (s, e) => radio.IsChecked = true; // Remove click handler
                    optionIdx++;
                }
            }

            // Count correct
            if (_userAnswers.TryGetValue(i, out int userAns) && userAns == q.CorrectIndex)
            {
                correct++;
            }
        }

        // Show result summary
        var percentage = questions.Count > 0 ? (correct * 100 / questions.Count) : 0;
        var minutes = _elapsedSeconds / 60;
        var seconds = _elapsedSeconds % 60;

        ScoreText.Text = $"{correct}/{questions.Count}";
        PercentText.Text = $"({percentage}%)";
        TimeResultText.Text = $"⏱️ Thời gian: {minutes:D2}:{seconds:D2}";

        ScoreText.Foreground = percentage >= 70 
            ? (System.Windows.Media.Brush)FindResource("AccentGreen") 
            : (System.Windows.Media.Brush)FindResource("AccentGold");

        if (percentage >= 70)
            ResultMessage.Text = "🎉 Tuyệt vời!";
        else if (percentage >= 50)
            ResultMessage.Text = "👍 Khá tốt!";
        else
            ResultMessage.Text = "💪 Cố gắng thêm nhé!";

        QuizResultPanel.Visibility = Visibility.Visible;
        CheckAnswersBtn.Visibility = Visibility.Collapsed;
        NextLessonBtn.Visibility = Visibility.Visible;
    }

    private void NextLesson_Click(object sender, RoutedEventArgs e)
    {
        GenerateButton_Click(sender, e);
    }
}

