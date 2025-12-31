using System.Windows;
using System.Windows.Controls;
using LinguaAI.Desktop.Services;

namespace LinguaAI.Desktop.Pages;

public partial class WritingPage : Page
{
    private readonly LinguaApiService _apiService;

    public WritingPage()
    {
        InitializeComponent();
        _apiService = new LinguaApiService();
    }

    private async void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        var text = WritingInput.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            System.Windows.MessageBox.Show("Vui lòng nhập nội dung cần kiểm tra.", "Thông báo");
            return;
        }

        var language = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ko";
        var level = (LevelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "intermediate";

        // Show loading
        EmptyState.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Collapsed;
        LoadingPanel.Visibility = Visibility.Visible;
        CheckButton.IsEnabled = false;
        WritingInput.IsReadOnly = true;

        try
        {
            var result = await _apiService.CheckWritingAsync(language, text, level);

            LoadingPanel.Visibility = Visibility.Collapsed;
            CheckButton.IsEnabled = true;
            WritingInput.IsReadOnly = false;

            if (result != null)
            {
                CorrectedText.Text = result.CorrectedText;
                CorrectionsList.ItemsSource = result.Corrections;
                SuggestionsList.ItemsSource = result.Suggestions;
                ResultPanel.Visibility = Visibility.Visible;
            }
            else
            {
                System.Windows.MessageBox.Show("Không thể kiểm tra bài viết. Vui lòng thử lại.", "Lỗi");
                EmptyState.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            CheckButton.IsEnabled = true;
            WritingInput.IsReadOnly = false;
            System.Windows.MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            EmptyState.Visibility = Visibility.Visible;
        }
    }
}
