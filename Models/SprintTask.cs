using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SprintTracker.Api.Models;

public class SprintTask
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("taskKey")]
    public string TaskKey { get; set; } = null!; // e.g., "PROJ-123"

    [BsonElement("projectId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProjectId { get; set; } = null!;

    [BsonElement("sprintId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? SprintId { get; set; } // null if in backlog

  [BsonElement("parentTaskId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ParentTaskId { get; set; } // For subtasks

    [BsonElement("title")]
    public string Title { get; set; } = null!;

    [BsonElement("description")]
    public string? Description { get; set; }

   [BsonElement("type")]
    public TaskType Type { get; set; } = TaskType.Story;

    [BsonElement("status")]
    public TaskStatus Status { get; set; } = TaskStatus.ToDo;

  [BsonElement("priority")]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [BsonElement("storyPoints")]
  public int? StoryPoints { get; set; }

    [BsonElement("estimatedHours")]
    public decimal? EstimatedHours { get; set; }

 [BsonElement("loggedHours")]
    public decimal LoggedHours { get; set; }

 [BsonElement("remainingHours")]
    public decimal? RemainingHours { get; set; }

    [BsonElement("assigneeId")]
 [BsonRepresentation(BsonType.ObjectId)]
    public string? AssigneeId { get; set; }

 [BsonElement("reporterId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ReporterId { get; set; } = null!;

    [BsonElement("labels")]
    public List<string> Labels { get; set; } = new();

    [BsonElement("acceptanceCriteria")]
    public List<AcceptanceCriterion> AcceptanceCriteria { get; set; } = new();

    [BsonElement("attachments")]
  public List<Attachment> Attachments { get; set; } = new();

    [BsonElement("blockedBy")]
    public List<string> BlockedByTaskIds { get; set; } = new();

    [BsonElement("dueDate")]
  public DateTime? DueDate { get; set; }

    [BsonElement("order")]
    public int Order { get; set; } // For ordering in backlog/sprint board

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
  public DateTime? CompletedAt { get; set; }
}

public class AcceptanceCriterion
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("description")]
    public string Description { get; set; } = null!;

    [BsonElement("isCompleted")]
    public bool IsCompleted { get; set; }
}

public class Attachment
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("fileName")]
    public string FileName { get; set; } = null!;

    [BsonElement("fileUrl")]
    public string FileUrl { get; set; } = null!;

    [BsonElement("fileSize")]
    public long FileSize { get; set; }

    [BsonElement("contentType")]
    public string ContentType { get; set; } = null!;

    [BsonElement("uploadedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UploadedBy { get; set; } = null!;

    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public enum TaskType
{
    Epic = 0,
    Story = 1,
    Task = 2,
Bug = 3,
    Subtask = 4,
    Spike = 5
}

public enum TaskStatus
{
    ToDo = 0,
    InProgress = 1,
    InReview = 2,
    Testing = 3,
    Done = 4,
    Blocked = 5
}

public enum TaskPriority
{
    Lowest = 0,
    Low = 1,
Medium = 2,
    High = 3,
    Highest = 4,
    Critical = 5
}
