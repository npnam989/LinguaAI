using System.Security.Cryptography;
using System.Text;

namespace LinguaAI.Api.Services;

public interface ITimeBasedAuthService
{
    string GeneratePassword(string apiKey, long utcTicks);
    string GenerateHash(string userId, string password);
    bool ValidateRequest(string authHeader, string expectedUserId, string expectedApiKey);
}

public class TimeBasedAuthService : ITimeBasedAuthService
{
    private const long WINDOW_TICKS = 600_000_000; // 60 seconds in ticks (1 tick = 100 nanoseconds)
    private readonly ILogger<TimeBasedAuthService> _logger;

    public TimeBasedAuthService(ILogger<TimeBasedAuthService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate time-based password using SHA256
    /// Password = SHA256(apiKey + timeWindow)
    /// </summary>
    public string GeneratePassword(string apiKey, long utcTicks)
    {
        var window = utcTicks / WINDOW_TICKS;
        var input = $"{apiKey}{window}";
        return ComputeSha256Hash(input);
    }

    /// <summary>
    /// Generate final hash using SHA256
    /// Hash = SHA256(userId:password)
    /// </summary>
    public string GenerateHash(string userId, string password)
    {
        var input = $"{userId}:{password}";
        return ComputeSha256Hash(input);
    }

    /// <summary>
    /// Validate Authorization header
    /// Format: HMAC-SHA256 {userId}:{hash}
    /// Allows 3 time windows: previous, current, next (for clock drift)
    /// </summary>
    public bool ValidateRequest(string authHeader, string expectedUserId, string expectedApiKey)
    {
        if (string.IsNullOrEmpty(authHeader))
            return false;

        try
        {
            // Parse header: "HMAC-SHA256 userId:hash"
            if (!authHeader.StartsWith("HMAC-SHA256 ", StringComparison.OrdinalIgnoreCase))
                return false;

            var credentials = authHeader.Substring("HMAC-SHA256 ".Length);
            var parts = credentials.Split(':', 2);
            
            if (parts.Length != 2)
                return false;

            var userId = parts[0];
            var receivedHash = parts[1];

            if (userId != expectedUserId)
                return false;

            // Check current, previous, and next time windows (allows 60s drift each direction)
            var currentTicks = DateTime.UtcNow.Ticks;
            
            for (int offset = -1; offset <= 1; offset++)
            {
                var windowTicks = currentTicks + (offset * WINDOW_TICKS);
                var password = GeneratePassword(expectedApiKey, windowTicks);
                var expectedHash = GenerateHash(expectedUserId, password);

                if (receivedHash == expectedHash)
                {
                    _logger.LogDebug("Auth validated with window offset: {Offset}", offset);
                    return true;
                }
            }

            _logger.LogWarning("Invalid auth hash from userId: {UserId}", userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating auth header");
            return false;
        }
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
