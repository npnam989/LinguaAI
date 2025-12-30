using LinguaAI.Api.Services;

namespace LinguaAI.Api.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    
    // Endpoints that don't require authentication
    private static readonly string[] ExcludedPaths = 
    {
        "/health",
        "/swagger",
        "/favicon.ico"
    };

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITimeBasedAuthService authService, IConfiguration config)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Skip auth for excluded paths
        if (ExcludedPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        // Get expected credentials from config
        var expectedUserId = config["Auth:UserId"];
        var expectedApiKey = config["Auth:ApiKey"];

        // If auth not configured, allow all (development mode)
        if (string.IsNullOrEmpty(expectedUserId) || string.IsNullOrEmpty(expectedApiKey))
        {
            _logger.LogWarning("Auth credentials not configured - allowing request without auth");
            await _next(context);
            return;
        }

        // Get Authorization header
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader))
        {
            _logger.LogWarning("Missing Authorization header for path: {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "Missing Authorization header" });
            return;
        }

        // Validate
        if (!authService.ValidateRequest(authHeader, expectedUserId, expectedApiKey))
        {
            _logger.LogWarning("Invalid Authorization for path: {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "Invalid or expired credentials" });
            return;
        }

        await _next(context);
    }
}

public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
