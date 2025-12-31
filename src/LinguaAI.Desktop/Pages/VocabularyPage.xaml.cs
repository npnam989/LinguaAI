using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LinguaAI.Common.Models;
using LinguaAI.Desktop.Services;

namespace LinguaAI.Desktop.Pages;

public partial class VocabularyPage : Page
{
    private readonly LinguaApiService _apiService;
    private List<VocabularyItem> _words = new();
    private int _currentIndex = 0;
    private bool _isFlipped = false;

    public VocabularyPage()
    {
        InitializeComponent();
        _apiService = new LinguaApiService();
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedLang = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ko";
        var selectedTheme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "greetings";
        var count = int.Parse((CountComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "10");

        // Show loading
        EmptyState.Visibility = Visibility.Collapsed;
        LoadingPanel.Visibility = Visibility.Visible;
        ModePanel.Visibility = Visibility.Collapsed;
        FlashcardArea.Visibility = Visibility.Collapsed;
        WordListArea.Visibility = Visibility.Collapsed;
        GenerateButton.IsEnabled = false;

        try
        {
            var result = await _apiService.GenerateVocabularyAsync(selectedLang, selectedTheme, count);
            
            LoadingPanel.Visibility = Visibility.Collapsed;
            GenerateButton.IsEnabled = true;

            if (result?.Words != null && result.Words.Count > 0)
            {
                _words = result.Words;
                _currentIndex = 0;
                _isFlipped = false;

                // Show mode selection
                ModePanel.Visibility = Visibility.Visible;
                
                // Default to flashcard mode
                ShowFlashcardMode();
            }
            else
            {
                System.Windows.MessageBox.Show("Không thể tải từ vựng. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmptyState.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            GenerateButton.IsEnabled = true;
            System.Windows.MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            EmptyState.Visibility = Visibility.Visible;
        }
    }

    private void FlashcardMode_Click(object sender, RoutedEventArgs e)
    {
        ShowFlashcardMode();
    }

    private void ListMode_Click(object sender, RoutedEventArgs e)
    {
        ShowListMode();
    }

    private void ShowFlashcardMode()
    {
        FlashcardModeBtn.Style = (Style)FindResource("PrimaryButton");
        ListModeBtn.Style = (Style)FindResource("SecondaryButton");
        
        FlashcardArea.Visibility = Visibility.Visible;
        WordListArea.Visibility = Visibility.Collapsed;
        
        UpdateFlashcard();
    }

    private void ShowListMode()
    {
        ListModeBtn.Style = (Style)FindResource("PrimaryButton");
        FlashcardModeBtn.Style = (Style)FindResource("SecondaryButton");
        
        FlashcardArea.Visibility = Visibility.Collapsed;
        WordListArea.Visibility = Visibility.Visible;
        
        VocabularyList.ItemsSource = _words;
        WordCountText.Text = _words.Count.ToString();
    }

    private void UpdateFlashcard()
    {
        if (_words.Count == 0) return;

        var word = _words[_currentIndex];
        CurrentIndexText.Text = (_currentIndex + 1).ToString();
        TotalCountText.Text = _words.Count.ToString();

        // Reset to front
        _isFlipped = false;
        ShowCardFront(word);
    }

    private void ShowCardFront(VocabularyItem word)
    {
        Flashcard.Style = (Style)FindResource("FlashcardFront");
        CardWord.Text = word.Word ?? "";
        CardWord.Foreground = System.Windows.Media.Brushes.White;
        CardPronunciation.Text = word.Pronunciation ?? "";
        CardPronunciation.Visibility = Visibility.Visible;
        CardMeaning.Visibility = Visibility.Collapsed;
        CardExample.Visibility = Visibility.Collapsed;
    }

    private void ShowCardBack(VocabularyItem word)
    {
        Flashcard.Style = (Style)FindResource("FlashcardBack");
        CardWord.Text = word.Word ?? "";
        CardWord.Foreground = (System.Windows.Media.Brush)FindResource("PrimaryText");
        CardPronunciation.Visibility = Visibility.Collapsed;
        CardMeaning.Text = word.Meaning ?? "";
        CardMeaning.Visibility = Visibility.Visible;
        CardExample.Text = word.Example ?? "";
        CardExample.Visibility = !string.IsNullOrEmpty(word.Example) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Flashcard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_words.Count == 0) return;

        _isFlipped = !_isFlipped;
        var word = _words[_currentIndex];

        if (_isFlipped)
            ShowCardBack(word);
        else
            ShowCardFront(word);
    }

    private void PrevCard_Click(object sender, RoutedEventArgs e)
    {
        if (_words.Count == 0) return;
        
        _currentIndex = (_currentIndex - 1 + _words.Count) % _words.Count;
        UpdateFlashcard();
    }

    private void NextCard_Click(object sender, RoutedEventArgs e)
    {
        if (_words.Count == 0) return;
        
        _currentIndex = (_currentIndex + 1) % _words.Count;
        UpdateFlashcard();
    }

    private void ShuffleCards_Click(object sender, RoutedEventArgs e)
    {
        if (_words.Count == 0) return;
        
        var random = new Random();
        _words = _words.OrderBy(_ => random.Next()).ToList();
        _currentIndex = 0;
        UpdateFlashcard();
    }
}
