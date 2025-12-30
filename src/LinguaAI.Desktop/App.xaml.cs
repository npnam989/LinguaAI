using System.Windows;
using Microsoft.Extensions.Hosting;

namespace LinguaAI.Desktop;

public partial class App : Application
{
    private IHost? _apiHost;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try 
        {
            // Start API on localhost:5278
            var args = new[] { "--urls=http://localhost:5278" };
            
            // Create and start the API host
            // Use LinguaAI.Api.Program.CreateApp
            var app = LinguaAI.Api.Program.CreateApp(args);
            _apiHost = app;
            
            await app.StartAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start API: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_apiHost != null)
        {
            await _apiHost.StopAsync();
            _apiHost.Dispose();
        }
        base.OnExit(e);
    }
}

