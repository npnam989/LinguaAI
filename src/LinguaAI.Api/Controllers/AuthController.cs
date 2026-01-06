using LinguaAI.Api.Services;
using LinguaAI.Common.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LinguaAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MongoService _mongoService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(MongoService mongoService, ILogger<AuthController> logger)
    {
        _mongoService = mongoService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username and password are required.");

        var existingUser = await _mongoService.Users.Find(u => u.Username == request.Username).FirstOrDefaultAsync();
        if (existingUser != null)
            return Conflict("Username already exists.");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password)), // Insecure but simple for now
            Email = request.Email
        };

        await _mongoService.Users.InsertOneAsync(user);

        // Remove password from response
        user.PasswordHash = "";
        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var passwordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password));
        var user = await _mongoService.Users.Find(u => u.Username == request.Username && u.PasswordHash == passwordHash).FirstOrDefaultAsync();

        if (user == null)
            return Unauthorized("Invalid credentials.");

        // In a real app, generate JWT here
        return Ok(new { Token = "dummy_token_" + user.Id, UserId = user.Id, Username = user.Username });
    }
}
