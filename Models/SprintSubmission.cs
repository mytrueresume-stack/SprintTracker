using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SprintTracker.Api.Models;

/// <summary>
/// Developer's submission for a sprint - contains all their work details
/// </summary>
public class SprintSubmission
{
  [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("sprintId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SprintId { get; set; } = null!;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
  public string UserId { get; set; } = null!;

    [BsonElement("projectId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProjectId { get; set; } = null!;

    // Work Details
    [BsonElement("storyPointsCompleted")]
    public int StoryPointsCompleted { get; set; }

    [BsonElement("storyPointsPlanned")]
    public int StoryPointsPlanned { get; set; }

    [BsonElement("hoursWorked")]
    public decimal HoursWorked { get; set; }

    // User Stories
    [BsonElement("userStories")]
    public List<UserStoryEntry> UserStories { get; set; } = new();

    // Features Delivered
    [BsonElement("featuresDelivered")]
    public List<FeatureEntry> FeaturesDelivered { get; set; } = new();

    // Impediments/Blockers
    [BsonElement("impediments")]
    public List<ImpedimentEntry> Impediments { get; set; } = new();

    // Appreciations
    [BsonElement("appreciations")]
    public List<AppreciationEntry> Appreciations { get; set; } = new();

    // Additional Notes
    [BsonElement("achievements")]
    public string? Achievements { get; set; }

    [BsonElement("learnings")]
    public string? Learnings { get; set; }

    [BsonElement("nextSprintGoals")]
    public string? NextSprintGoals { get; set; }

    [BsonElement("additionalNotes")]
    public string? AdditionalNotes { get; set; }

    // Status
    [BsonElement("submissionStatus")]
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Draft;

    [BsonElement("submittedAt")]
    public DateTime? SubmittedAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class UserStoryEntry
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("storyId")]
    public string StoryId { get; set; } = null!; // e.g., "US-123"

    [BsonElement("title")]
    public string Title { get; set; } = null!;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("storyPoints")]
    public int StoryPoints { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "Completed"; // Completed, In Progress, Carry Forward

    [BsonElement("remarks")]
    public string? Remarks { get; set; }
}

public class FeatureEntry
{
    [BsonElement("id")]
  public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("featureName")]
    public string FeatureName { get; set; } = null!;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("module")]
  public string? Module { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "Delivered"; // Delivered, Partially Delivered, Pending
}

public class ImpedimentEntry
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("description")]
    public string Description { get; set; } = null!;

    [BsonElement("category")]
    public string Category { get; set; } = "Technical"; // Technical, Resource, Process, External

    [BsonElement("impact")]
    public string Impact { get; set; } = "Medium"; // Low, Medium, High, Critical

    [BsonElement("status")]
    public string Status { get; set; } = "Open"; // Open, Resolved, Escalated

    [BsonElement("resolution")]
    public string? Resolution { get; set; }

    [BsonElement("reportedDate")]
    public DateTime ReportedDate { get; set; } = DateTime.UtcNow;

    [BsonElement("resolvedDate")]
    public DateTime? ResolvedDate { get; set; }
}

public class AppreciationEntry
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("appreciatedUserId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AppreciatedUserId { get; set; } // Can be null for team appreciation

[BsonElement("appreciatedUserName")]
    public string AppreciatedUserName { get; set; } = null!;

    [BsonElement("reason")]
    public string Reason { get; set; } = null!;

    [BsonElement("category")]
    public string Category { get; set; } = "Teamwork"; // Teamwork, Technical Excellence, Innovation, Mentoring, Going Extra Mile
}

public enum SubmissionStatus
{
    Draft = 0,
    Submitted = 1,
    Reviewed = 2
}
