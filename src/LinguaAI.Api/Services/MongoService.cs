using LinguaAI.Common.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace LinguaAI.Api.Services;

public class MongoService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoService> _logger;

    public MongoService(IConfiguration configuration, ILogger<MongoService> logger)
    {
        _logger = logger;
        
        var encryptedConnectionString = configuration.GetConnectionString("LinguaAIDBConnectionString");
        var encryptionKey = configuration["EncryptionKey"]; // From appsettings.Development.json or Env Var

        if (string.IsNullOrEmpty(encryptedConnectionString) || string.IsNullOrEmpty(encryptionKey))
        {
            _logger.LogError("Missing ConnectionString or EncryptionKey");
            throw new Exception("Database configuration is missing.");
        }

        string connectionString;
        try
        {
             connectionString = DecryptString(encryptionKey, encryptedConnectionString);
             
             // Use MongoUrlBuilder to safely manage connection string parameters
             var mongoUrlBuilder = new MongoUrlBuilder(connectionString);
             if (string.IsNullOrEmpty(mongoUrlBuilder.AuthenticationSource))
             {
                 mongoUrlBuilder.AuthenticationSource = "admin";
             }
             connectionString = mongoUrlBuilder.ToString();

             // Debug log to verify string (mask password)
            //  var masked = System.Text.RegularExpressions.Regex.Replace(connectionString, "(:)([^@]+)(@)", "$1***$3");
             _logger.LogInformation("Decrypted Connection String: {Masked}", connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt connection string");
            throw;
        }

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("lingua_ai_db");
        _logger.LogInformation("Connected to MongoDB");
        
        // Helper to log public IP for Whitelisting
        Task.Run(async () => {
            try {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                var ip = await httpClient.GetStringAsync("https://api.ipify.org");
                _logger.LogWarning("=================================================");
                _logger.LogWarning("CURRENT PUBLIC IP: {IP}", ip);
                _logger.LogWarning("Add this IP to MongoDB Atlas -> Network Access");
                _logger.LogWarning("=================================================");
            } catch (Exception ex) {
                _logger.LogError("Could not fetch Public IP: {Message}", ex.Message);
            }
        });
    }

    private string DecryptString(string key, string cipherText)
    {
        byte[] fullCipher = Convert.FromBase64String(cipherText);
        
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key);
            
            byte[] iv = new byte[16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream msDecrypt = new MemoryStream(fullCipher, 16, fullCipher.Length - 16))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<UserActionLog> ActionLogs => _database.GetCollection<UserActionLog>("action_logs");
    public IMongoCollection<AIResponseLog> AILogs => _database.GetCollection<AIResponseLog>("ai_logs");

    // Generic Helper (optional)
    public async Task LogActionAsync(string userId, string action, string details)
    {
        try
        {
            var log = new UserActionLog 
            { 
                UserId = userId, 
                ActionType = action, 
                Details = details 
            };
            await ActionLogs.InsertOneAsync(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user action");
        }
    }

    public async Task LogAIResponseAsync(string userId, string prompt, string response, string type)
    {
        try
        {
            var log = new AIResponseLog
            {
                UserId = userId,
                RequestPrompt = prompt,
                AIResponse = response,
                RequestType = type
            };
            await AILogs.InsertOneAsync(log);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to log AI response");
        }
    }
}
