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
}
