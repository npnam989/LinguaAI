using System.Windows;
using Microsoft.Extensions.Hosting;

namespace LinguaAI.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _apiHost;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try 
        {
            // 1. Get obfuscated credentials for Railway API
            var (userId, apiKey) = Services.CredentialProtector.GetCredentials();
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(apiKey))
            {
                System.Windows.MessageBox.Show("API credentials not configured. Please contact support.", "Configuration Error");
                Shutdown();
                return;
            }

            // 2. Inject into Environment for the API service to use
            Environment.SetEnvironmentVariable("Auth__UserId", userId);
            Environment.SetEnvironmentVariable("Auth__ApiKey", apiKey);

            // 3. Railway API URL (no local API needed)
            // Note: We don't start a local API host when using Railway
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to initialize: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}

