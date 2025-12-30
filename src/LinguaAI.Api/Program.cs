using LinguaAI.Api.Services;
using LinguaAI.Api.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace LinguaAI.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var app = CreateApp(args);
        app.Run();
    }

    public static WebApplication CreateApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // Swagger - enabled for all environments
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "LinguaAI API", Version = "v1" });
        });

        // Register services
        builder.Services.AddSingleton<IGeminiService, GeminiService>();
        builder.Services.AddSingleton<IFileParserService, FileParserService>();
        builder.Services.AddSingleton<ITimeBasedAuthService, TimeBasedAuthService>();

        // Rate Limiting - protect against abuse
        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 30, // 30 requests per window
                        Window = TimeSpan.FromMinutes(1)
                    }));
            
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // CORS - configurable via environment
        var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',') 
            ?? new[] { "http://localhost:5262", "https://localhost:5262" };

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowWeb", policy =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
                else
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            });
        });

        var app = builder.Build();

        // Security headers
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            await next();
        });

        // Swagger UI - Protected by Basic Auth
        app.UseMiddleware<SwaggerBasicAuthMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "LinguaAI API v1");
            c.RoutePrefix = "swagger";
        });

        // HTTPS redirection in production
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseRateLimiter();
        app.UseCors("AllowWeb");
        app.UseApiKeyAuth(); // Time-based API key authentication
        app.MapControllers();

        return app;
    }
}
