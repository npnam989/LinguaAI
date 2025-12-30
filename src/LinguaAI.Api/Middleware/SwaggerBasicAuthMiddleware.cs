using System.Net.Http.Headers;
using System.Text;

namespace LinguaAI.Api.Middleware;

public class SwaggerBasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<SwaggerBasicAuthMiddleware> _logger;

    public SwaggerBasicAuthMiddleware(RequestDelegate next, IConfiguration config, ILogger<SwaggerBasicAuthMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            if (await IsAuthorized(context))
            {
                await _next(context);
                return;
            }

            context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"LinguaAI Swagger\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }

    private async Task<bool> IsAuthorized(HttpContext context)
    {
        string username = _config["Swagger:User"] ?? "admin";
        string password = _config["Swagger:Password"] ?? "password";

        // If no auth configured, block or allow? Safer to require auth.
        // But for simplicity if env var not set we might default to something known or disable.
        // Let's assume we want to enforce it.

        try
        {
            string authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader)) return false;

            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);

            if (authHeaderVal.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase) &&
                authHeaderVal.Parameter != null)
            {
                var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeaderVal.Parameter)).Split(':', 2);
                var user = credentials[0];
                var pass = credentials[1];

                return user == username && pass == password;
            }
        }
        catch
        {
            // Invalid header
            return false;
        }

        return false;
    }
}
