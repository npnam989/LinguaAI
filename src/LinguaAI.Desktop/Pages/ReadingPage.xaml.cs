using System.Windows;
using System.Windows.Controls;
using LinguaAI.Desktop.Services;

namespace LinguaAI.Desktop.Pages;

public partial class ReadingPage : Page
{
    private readonly LinguaApiService _apiService;

    public ReadingPage()
    {
        InitializeComponent();
        _apiService = new LinguaApiService();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.GoBack();
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        var level = (LevelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "intermediate";
        var topic = TopicInput.Text.Trim();

        if (string.IsNullOrEmpty(topic))
        {
            System.Windows.MessageBox.Show("Please enter a topic.");
            return;
        }

        LoadingBar.Visibility = Visibility.Visible;
        GenerateButton.IsEnabled = false;

        var result = await _apiService.GenerateReadingAsync("en", level, topic);

        LoadingBar.Visibility = Visibility.Collapsed;
        GenerateButton.IsEnabled = true;

        if (result != null)
        {
            TitleText.Text = result.Title;
            ContentText.Text = result.Content;
            VocabList.ItemsSource = result.Vocabulary;
            QuizList.ItemsSource = result.Questions;
        }
        else
        {
            System.Windows.MessageBox.Show("Failed to generate reading.");
        }
    }
}
