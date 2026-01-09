using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SprintTracker.Api.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("email")]
    public string Email { get; set; } = null!;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = null!;

    [BsonElement("firstName")]
    public string FirstName { get; set; } = null!;

    [BsonElement("lastName")]
    public string LastName { get; set; } = null!;

    [BsonElement("role")]
    public UserRole Role { get; set; } = UserRole.Developer;

    [BsonElement("avatar")]
    public string? Avatar { get; set; }

    [BsonElement("teamId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? TeamId { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [BsonIgnore]
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Check if user has manager-level access (Admin or Manager)
    /// </summary>
    [BsonIgnore]
    public bool IsManager => Role == UserRole.Admin || Role == UserRole.Manager;
}

/// <summary>
/// Simplified user roles: Admin, Manager, Developer only
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Full system access - can manage all users, projects, and settings
    /// </summary>
    Admin = 0,
    
    /// <summary>
    /// Can create projects, sprints, and view team reports
    /// </summary>
    Manager = 1,
    
    /// <summary>
    /// Can view projects, submit sprint details, and manage own tasks
    /// </summary>
    Developer = 2
}
