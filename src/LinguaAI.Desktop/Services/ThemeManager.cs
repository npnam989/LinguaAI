using System.Windows;

namespace LinguaAI.Desktop.Services;

public static class ThemeManager
{
    public enum Theme { Light, Dark }

    public static void SetTheme(Theme theme)
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        var merged = app.Resources.MergedDictionaries;
        merged.Clear();

        string themeFile = theme == Theme.Light ? "Themes/Light.xaml" : "Themes/Dark.xaml";
        
        merged.Add(new ResourceDictionary { Source = new Uri(themeFile, UriKind.Relative) });
        merged.Add(new ResourceDictionary { Source = new Uri("Themes/Styles.xaml", UriKind.Relative) });
    }
}
