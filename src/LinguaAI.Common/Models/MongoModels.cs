using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LinguaAI.Common.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = ""; // Simple hash for demo
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserActionLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    public string UserId { get; set; } = ""; // Optional if anon
    public string ActionType { get; set; } = ""; // e.g. "GeneratePractice", "CheckTranslation"
    public string Details { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AIResponseLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    public string UserId { get; set; } = "";
    public string RequestPrompt { get; set; } = "";
    public string AIResponse { get; set; } = "";
    public string RequestType { get; set; } = ""; // e.g. "Practice", "Vocabulary"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Email { get; set; } = "";
}
