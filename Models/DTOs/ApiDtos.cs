namespace SprintTracker.Api.Models.DTOs;

// Authentication DTOs
public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role = UserRole.Developer
);

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    UserRole Role,
    string? Avatar,
    bool IsActive
);

// Project DTOs
public record CreateProjectRequest(
    string Name,
    string Key,
    string? Description,
    DateTime? StartDate,
    DateTime? TargetEndDate
);

public record UpdateProjectRequest(
  string? Name,
    string? Description,
    ProjectStatus? Status,
    DateTime? TargetEndDate,
    List<string>? TeamMemberIds
);

public record ProjectDto(
    string Id,
    string Name,
    string Key,
    string? Description,
    string OwnerId,
    List<UserDto> TeamMembers,
    ProjectStatus Status,
    DateTime? StartDate,
    DateTime? TargetEndDate,
    ProjectSettings Settings,
    DateTime CreatedAt
);

// Sprint DTOs
public record CreateSprintRequest(
    string ProjectId,
    string Name,
    string? Goal,
    DateTime StartDate,
    DateTime EndDate
);

public record UpdateSprintRequest(
string? Name,
string? Goal,
    SprintStatus? Status,
    DateTime? StartDate,
    DateTime? EndDate,
    SprintCapacity? Capacity
);

public record SprintDto(
  string Id,
    string ProjectId,
    string Name,
    string? Goal,
    int SprintNumber,
    SprintStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    SprintCapacity Capacity,
    int? ActualVelocity,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    SprintRetrospective? Retrospective,
    SprintStats Stats
);

public record SprintStats(
    int TotalTasks,
    int CompletedTasks,
    int TotalStoryPoints,
 int CompletedStoryPoints,
    decimal CompletionPercentage
);

// Task DTOs
public record CreateTaskRequest(
    string ProjectId,
    string? SprintId,
    string Title,
    string? Description,
    TaskType Type,
  TaskPriority Priority,
    int? StoryPoints,
  decimal? EstimatedHours,
 string? AssigneeId,
    string? ParentTaskId,
    List<string>? Labels,
    List<string>? AcceptanceCriteria,
    DateTime? DueDate
);

public record UpdateTaskRequest(
    string? Title,
    string? Description,
    TaskType? Type,
    TaskStatus? Status,
    TaskPriority? Priority,
    int? StoryPoints,
    decimal? EstimatedHours,
    decimal? RemainingHours,
    string? AssigneeId,
    string? SprintId,
    List<string>? Labels,
    DateTime? DueDate,
    int? Order
);

public record TaskDto(
    string Id,
    string TaskKey,
    string ProjectId,
    string? SprintId,
    string? ParentTaskId,
    string Title,
    string? Description,
    TaskType Type,
    TaskStatus Status,
    TaskPriority Priority,
    int? StoryPoints,
    decimal? EstimatedHours,
    decimal LoggedHours,
    decimal? RemainingHours,
    UserDto? Assignee,
    UserDto Reporter,
    List<string> Labels,
    List<AcceptanceCriterion> AcceptanceCriteria,
    List<string> BlockedByTaskIds,
    DateTime? DueDate,
    int Order,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    List<TaskDto>? Subtasks
);

public record LogTimeRequest(decimal Hours, string? Description);

public record MoveTaskRequest(string? SprintId, int Order);

// Comment DTOs
public record CreateCommentRequest(string Content, List<string>? MentionedUserIds);

public record CommentDto(
string Id,
    string TaskId,
    UserDto Author,
    string Content,
    List<string> MentionedUserIds,
    bool IsEdited,
    DateTime CreatedAt
);

// Dashboard/Analytics DTOs
public record DashboardStats(
    int TotalProjects,
    int ActiveSprints,
    int TotalTasks,
    int TasksInProgress,
    int TasksCompleted,
    int TasksBlocked,
    List<SprintSummary> RecentSprints,
    List<TaskDto> MyTasks
);

public record SprintSummary(
  string Id,
 string Name,
    string ProjectName,
    SprintStatus Status,
    DateTime EndDate,
    decimal CompletionPercentage,
    int DaysRemaining,
    string? CompletionSource
);

public record BurndownData(
    string SprintId,
    List<BurndownPoint> IdealBurndown,
    List<BurndownPoint> ActualBurndown
);

public record BurndownPoint(DateTime Date, decimal Points);

public record VelocityData(
    string ProjectId,
    List<SprintVelocity> Velocities,
    decimal AverageVelocity
);

public record SprintVelocity(string SprintName, int CommittedPoints, int CompletedPoints);

// Common DTOs
public record ApiResponse<T>(bool Success, T? Data, string? Message, List<string>? Errors);

public record PaginatedResponse<T>(
  List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
