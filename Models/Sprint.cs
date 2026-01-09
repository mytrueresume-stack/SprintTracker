using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SprintTracker.Api.Models;

public class Sprint
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("projectId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProjectId { get; set; } = null!;

 [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("goal")]
    public string? Goal { get; set; }

    [BsonElement("sprintNumber")]
    public int SprintNumber { get; set; }

    [BsonElement("status")]
    public SprintStatus Status { get; set; } = SprintStatus.Planning;

    [BsonElement("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("endDate")]
    public DateTime EndDate { get; set; }

  [BsonElement("capacity")]
    public SprintCapacity Capacity { get; set; } = new();

    [BsonElement("velocity")]
    public int? ActualVelocity { get; set; }

    [BsonElement("createdBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CreatedBy { get; set; } = null!;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("startedAt")]
    public DateTime? StartedAt { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("retrospective")]
    public SprintRetrospective? Retrospective { get; set; }
}

public class SprintCapacity
{
    [BsonElement("plannedPoints")]
    public int PlannedStoryPoints { get; set; }

    [BsonElement("committedPoints")]
    public int CommittedStoryPoints { get; set; }

    [BsonElement("totalHours")]
    public decimal TotalAvailableHours { get; set; }

 [BsonElement("memberCapacities")]
    public List<MemberCapacity> MemberCapacities { get; set; } = new();
}

public class MemberCapacity
{
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    [BsonElement("availableHours")]
    public decimal AvailableHours { get; set; }

 [BsonElement("daysOff")]
    public List<DateTime> DaysOff { get; set; } = new();
}

public class SprintRetrospective
{
    [BsonElement("whatWentWell")]
    public List<string> WhatWentWell { get; set; } = new();

    [BsonElement("whatCouldImprove")]
    public List<string> WhatCouldImprove { get; set; } = new();

  [BsonElement("actionItems")]
    public List<string> ActionItems { get; set; } = new();

    [BsonElement("teamMorale")]
    public int TeamMorale { get; set; } // 1-5 scale

    [BsonElement("notes")]
public string? Notes { get; set; }
}

public enum SprintStatus
{
    Planning = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}
