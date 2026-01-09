using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SprintTracker.Api.Models;

public class Project
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("key")]
    public string Key { get; set; } = null!; // e.g., "PROJ" for PROJ-123 task IDs

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("ownerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OwnerId { get; set; } = null!;

    [BsonElement("teamMembers")]
    public List<string> TeamMemberIds { get; set; } = new();

    [BsonElement("status")]
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    [BsonElement("startDate")]
    public DateTime? StartDate { get; set; }

    [BsonElement("targetEndDate")]
    public DateTime? TargetEndDate { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("settings")]
    public ProjectSettings Settings { get; set; } = new();
}

public class ProjectSettings
{
    [BsonElement("defaultSprintDuration")]
    public int DefaultSprintDurationDays { get; set; } = 14;

    [BsonElement("workingDays")]
    public List<DayOfWeek> WorkingDays { get; set; } = new() 
    { 
        DayOfWeek.Monday, 
     DayOfWeek.Tuesday, 
        DayOfWeek.Wednesday, 
 DayOfWeek.Thursday, 
        DayOfWeek.Friday 
    };

    [BsonElement("estimationUnit")]
    public EstimationUnit EstimationUnit { get; set; } = EstimationUnit.StoryPoints;
}

public enum ProjectStatus
{
    Active = 0,
    OnHold = 1,
    Completed = 2,
    Archived = 3
}

public enum EstimationUnit
{
    StoryPoints = 0,
    Hours = 1,
    Days = 2
}
