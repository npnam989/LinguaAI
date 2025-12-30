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
            // 1. Generate Ephemeral Credentials for this session
            // This ensures the API Key is never stored on disk and changes every run.
            // "Config trực tiếp trong ứng dụng" -> Configured dynamically in memory.
            string userId = "desktop_user_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string apiKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // Strong random key

            // 2. Inject into Environment for the API Host to pick up (via IConfiguration)
            Environment.SetEnvironmentVariable("Auth__UserId", userId);
            Environment.SetEnvironmentVariable("Auth__ApiKey", apiKey);

            // 3. Find a free port dynamically to avoid "Address already in use"
            int port = GetAvailablePort();
            string url = $"http://localhost:{port}";
            
            // 4. Update Client Configuration
            Services.LinguaApiService.BaseUrl = url;

            // 5. Start API on the dynamic port
            var args = new[] { $"--urls={url}" };
            
            // Create and start the API host
            // Use LinguaAI.Api.Program.CreateApp
            var app = LinguaAI.Api.Program.CreateApp(args);
            _apiHost = app;
            
            await app.StartAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to start API: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Shutdown();
        }
    }

    private int GetAvailablePort()
    {
        using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp))
        {
            socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
            return ((System.Net.IPEndPoint)socket.LocalEndPoint!).Port;
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

