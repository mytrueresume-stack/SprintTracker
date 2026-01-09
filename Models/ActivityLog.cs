using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SprintTracker.Api.Models;

public class Comment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

 [BsonElement("taskId")]
  [BsonRepresentation(BsonType.ObjectId)]
    public string TaskId { get; set; } = null!;

    [BsonElement("authorId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AuthorId { get; set; } = null!;

    [BsonElement("content")]
    public string Content { get; set; } = null!;

    [BsonElement("mentions")]
    public List<string> MentionedUserIds { get; set; } = new();

    [BsonElement("isEdited")]
    public bool IsEdited { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ActivityLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
  public string Id { get; set; } = null!;

    [BsonElement("entityType")]
    public string EntityType { get; set; } = null!; // Task, Sprint, Project

    [BsonElement("entityId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string EntityId { get; set; } = null!;

 [BsonElement("action")]
    public string Action { get; set; } = null!; // Created, Updated, StatusChanged, etc.

 [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    [BsonElement("changes")]
    public List<FieldChange> Changes { get; set; } = new();

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class FieldChange
{
    [BsonElement("field")]
    public string FieldName { get; set; } = null!;

    [BsonElement("oldValue")]
    public string? OldValue { get; set; }

    [BsonElement("newValue")]
    public string? NewValue { get; set; }
}

// Time-series metrics for sprint analytics
public class SprintMetrics
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("sprintId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SprintId { get; set; } = null!;

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("totalPoints")]
    public int TotalStoryPoints { get; set; }

  [BsonElement("completedPoints")]
  public int CompletedStoryPoints { get; set; }

    [BsonElement("remainingPoints")]
    public int RemainingStoryPoints { get; set; }

    [BsonElement("tasksByStatus")]
    public Dictionary<string, int> TasksByStatus { get; set; } = new();

    [BsonElement("burndownIdeal")]
  public decimal IdealBurndown { get; set; }

    [BsonElement("burndownActual")]
    public decimal ActualBurndown { get; set; }

    [BsonElement("velocity")]
    public decimal DailyVelocity { get; set; }
}
