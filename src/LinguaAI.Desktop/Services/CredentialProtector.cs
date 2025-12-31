using System.Security.Cryptography;
using System.Text;

namespace LinguaAI.Desktop.Services;

/// <summary>
/// Simple credential obfuscation to prevent casual inspection.
/// NOTE: This is NOT cryptographically secure against determined attackers,
/// but provides a reasonable barrier for distribution.
/// </summary>
public static class CredentialProtector
{
    // Obfuscation key derived from app-specific data
    // This makes it harder (but not impossible) to extract credentials
    private static readonly byte[] Key = SHA256.HashData(
        Encoding.UTF8.GetBytes("LinguaAI-Desktop-2024-SecretSalt-v1"));
    
    private static readonly byte[] IV = MD5.HashData(
        Encoding.UTF8.GetBytes("LinguaAI-IV-Salt"));

    /// <summary>
    /// Encrypt a string for storage in source code.
    /// Run this once to generate encrypted values, then paste into GetCredentials().
    /// </summary>
    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypt an obfuscated string at runtime.
    /// </summary>
    public static string Decrypt(string encryptedBase64)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        
        using var decryptor = aes.CreateDecryptor();
        var encryptedBytes = Convert.FromBase64String(encryptedBase64);
        var plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Get the Railway API credentials.
    /// To update: Run Encrypt() with your credentials, then paste the result here.
    /// </summary>
    public static (string UserId, string ApiKey) GetCredentials()
    {
        // TODO: Replace these with your encrypted Railway credentials
        // Use the Encrypt() method to generate these values, then paste here.
        // Example usage (run once in debug):
        //   var encUserId = CredentialProtector.Encrypt("your_railway_user_id");
        //   var encApiKey = CredentialProtector.Encrypt("your_railway_api_key");
        
        const string encryptedUserId = "15qXQ4qchiXWiPTGWioxCA==";
        const string encryptedApiKey = "VMSU4ysc6a1jzZf+Fc8vB2VPy2Y3d2OQX9JgMx59Agpcp6I+8cWHg0les3mYlCXE";

        try
        {
            return (Decrypt(encryptedUserId), Decrypt(encryptedApiKey));
        }
        catch
        {
            // Fallback if decryption fails (credentials not yet configured)
            return (string.Empty, string.Empty);
        }
    }
}
