using System;
using System.Security.Cryptography;
using System.Text;

class Program
{
    private static readonly byte[] Key = SHA256.HashData(
        Encoding.UTF8.GetBytes("LinguaAI-Desktop-2024-SecretSalt-v1"));
    
    private static readonly byte[] IV = MD5.HashData(
        Encoding.UTF8.GetBytes("LinguaAI-IV-Salt"));

    static void Main()
    {
        string userId = "namnp";
        string apiKey = "a8f921bc4d7e35a109fe67b2cd568142";

        Console.WriteLine("Encrypted UserId: " + Encrypt(userId));
        Console.WriteLine("Encrypted ApiKey: " + Encrypt(apiKey));
    }

    static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }
}
