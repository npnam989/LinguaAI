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

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.GoBack();
    }

    private async void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        var text = WritingInput.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            System.Windows.MessageBox.Show("Please enter some text to check.");
            return;
        }

        var level = (LevelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "intermediate";

        LoadingBar.Visibility = Visibility.Visible;
        CheckButton.IsEnabled = false;
        WritingInput.IsReadOnly = true;

        var result = await _apiService.CheckWritingAsync("en", text, level);

        LoadingBar.Visibility = Visibility.Collapsed;
        CheckButton.IsEnabled = true;
        WritingInput.IsReadOnly = false;

        if (result != null)
        {
            CorrectedText.Text = result.CorrectedText;
            CorrectionsList.ItemsSource = result.Corrections;
            SuggestionsList.ItemsSource = result.Suggestions;
        }
        else
        {
            System.Windows.MessageBox.Show("Failed to check writing. API error.");
        }
    }
}
