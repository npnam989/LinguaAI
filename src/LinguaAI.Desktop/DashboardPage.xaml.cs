using System.Windows;
using System.Windows.Controls;

namespace LinguaAI.Desktop;

public partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private void PracticeButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new Pages.PronunciationPage());
    }

    private void VocabularyButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new Pages.VocabularyPage());
    }

    private void ConversationButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new Pages.ConversationPage());
    }

    private void ReadingButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new Pages.ReadingPage());
    }

    private void WritingButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new Pages.WritingPage());
    }

    private bool _isDark = false;
    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        Services.ThemeManager.SetTheme(_isDark ? Services.ThemeManager.Theme.Dark : Services.ThemeManager.Theme.Light);
        if (sender is System.Windows.Controls.Primitives.ToggleButton toggle)
        {
            toggle.Content = _isDark ? "☀️ Light Mode" : "🌙 Dark Mode";
        }
    }
}
