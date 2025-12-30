using System.Windows;
using System.Windows.Controls;
using LinguaAI.Common.Models;
using LinguaAI.Desktop.Services;

namespace LinguaAI.Desktop.Pages;

public partial class VocabularyPage : Page
{
    private readonly LinguaApiService _apiService;

    public VocabularyPage()
    {
        InitializeComponent();
        _apiService = new LinguaApiService();
        LoadThemes();
    }

    private async void LoadThemes()
    {
        var themes = await _apiService.GetThemesAsync();
        ThemeComboBox.ItemsSource = themes;
        if (themes.Count > 0) ThemeComboBox.SelectedIndex = 0;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.GoBack();
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedTheme = ThemeComboBox.SelectedValue as string;
        if (string.IsNullOrEmpty(selectedTheme)) return;

        if (selectedTheme == "custom")
        {
            System.Windows.MessageBox.Show("File upload feature coming soon to Desktop.", "Info");
            return;
        }

        LoadingBar.Visibility = Visibility.Visible;
        VocabularyList.ItemsSource = null;
        GenerateButton.IsEnabled = false;

        var result = await _apiService.GenerateVocabularyAsync("en", selectedTheme, 10);
        
        LoadingBar.Visibility = Visibility.Collapsed;
        GenerateButton.IsEnabled = true;

        if (result != null)
        {
            VocabularyList.ItemsSource = result.Words;
        }
        else
        {
            System.Windows.MessageBox.Show("Failed to load vocabulary.", "Error");
        }
    }
}
