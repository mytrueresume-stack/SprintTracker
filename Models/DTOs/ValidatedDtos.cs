using System.ComponentModel.DataAnnotations;
using SprintTracker.Api.Models;

namespace SprintTracker.Api.Models.DTOs;

// Authentication DTOs with Validation
public record RegisterRequestDto
{
    [Required(ErrorMessage = "Email is required")]
  [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string Email { get; init; } = null!;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string Password { get; init; } = null!;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
    public string FirstName { get; init; } = null!;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
    public string LastName { get; init; } = null!;

    public UserRole Role { get; init; } = UserRole.Developer;
}

public record LoginRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = null!;

[Required(ErrorMessage = "Password is required")]
    public string Password { get; init; } = null!;
}

// Project DTOs with Validation
public record CreateProjectRequestDto
{
    [Required(ErrorMessage = "Project name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Project name must be between 2 and 200 characters")]
    public string Name { get; init; } = null!;

    [Required(ErrorMessage = "Project key is required")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "Project key must be between 2 and 10 characters")]
    [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Project key must contain only uppercase letters and numbers")]
    public string Key { get; init; } = null!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; init; }

    public DateTime? StartDate { get; init; }

    [CustomValidation(typeof(DateValidators), nameof(DateValidators.ValidateTargetEndDate))]
    public DateTime? TargetEndDate { get; init; }
}

public record UpdateProjectRequestDto
{
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Project name must be between 2 and 200 characters")]
    public string? Name { get; init; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; init; }

    public ProjectStatus? Status { get; init; }

    public DateTime? TargetEndDate { get; init; }

    public List<string>? TeamMemberIds { get; init; }
}

// Sprint DTOs with Validation
public record CreateSprintRequestDto
{
    [Required(ErrorMessage = "Project ID is required")]
    public string ProjectId { get; init; } = null!;

    [Required(ErrorMessage = "Sprint name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Sprint name must be between 2 and 200 characters")]
    public string Name { get; init; } = null!;

    [StringLength(1000, ErrorMessage = "Sprint goal cannot exceed 1000 characters")]
    public string? Goal { get; init; }

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; init; }

    [Required(ErrorMessage = "End date is required")]
    [CustomValidation(typeof(DateValidators), nameof(DateValidators.ValidateSprintEndDate))]
    public DateTime EndDate { get; init; }
}

public record UpdateSprintRequestDto
{
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Sprint name must be between 2 and 200 characters")]
    public string? Name { get; init; }

    [StringLength(1000, ErrorMessage = "Sprint goal cannot exceed 1000 characters")]
  public string? Goal { get; init; }

    public SprintStatus? Status { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public SprintCapacity? Capacity { get; init; }
}

// Task DTOs with Validation
public record CreateTaskRequestDto
{
 [Required(ErrorMessage = "Project ID is required")]
    public string ProjectId { get; init; } = null!;

    public string? SprintId { get; init; }

    [Required(ErrorMessage = "Task title is required")]
  [StringLength(500, MinimumLength = 3, ErrorMessage = "Task title must be between 3 and 500 characters")]
    public string Title { get; init; } = null!;

    [StringLength(10000, ErrorMessage = "Description cannot exceed 10000 characters")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "Task type is required")]
    public TaskType Type { get; init; }

    [Required(ErrorMessage = "Task priority is required")]
    public TaskPriority Priority { get; init; }

    [Range(0, 100, ErrorMessage = "Story points must be between 0 and 100")]
    public int? StoryPoints { get; init; }

    [Range(0, 1000, ErrorMessage = "Estimated hours must be between 0 and 1000")]
    public decimal? EstimatedHours { get; init; }

    public string? AssigneeId { get; init; }

    public string? ParentTaskId { get; init; }

    [MaxLength(20, ErrorMessage = "Cannot have more than 20 labels")]
    public List<string>? Labels { get; init; }

    [MaxLength(50, ErrorMessage = "Cannot have more than 50 acceptance criteria")]
    public List<string>? AcceptanceCriteria { get; init; }

    public DateTime? DueDate { get; init; }
}

public record UpdateTaskRequestDto
{
    [StringLength(500, MinimumLength = 3, ErrorMessage = "Task title must be between 3 and 500 characters")]
    public string? Title { get; init; }

    [StringLength(10000, ErrorMessage = "Description cannot exceed 10000 characters")]
  public string? Description { get; init; }

    public TaskType? Type { get; init; }

    public TaskStatus? Status { get; init; }

    public TaskPriority? Priority { get; init; }

    [Range(0, 100, ErrorMessage = "Story points must be between 0 and 100")]
    public int? StoryPoints { get; init; }

    [Range(0, 1000, ErrorMessage = "Estimated hours must be between 0 and 1000")]
    public decimal? EstimatedHours { get; init; }

    [Range(0, 1000, ErrorMessage = "Remaining hours must be between 0 and 1000")]
    public decimal? RemainingHours { get; init; }

    public string? AssigneeId { get; init; }

    public string? SprintId { get; init; }

    [MaxLength(20, ErrorMessage = "Cannot have more than 20 labels")]
    public List<string>? Labels { get; init; }

    public DateTime? DueDate { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "Order must be a non-negative number")]
    public int? Order { get; init; }
}

public record LogTimeRequestDto
{
    [Required(ErrorMessage = "Hours are required")]
    [Range(0.1, 24, ErrorMessage = "Hours must be between 0.1 and 24")]
    public decimal Hours { get; init; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; init; }
}

public record MoveTaskRequestDto
{
    public string? SprintId { get; init; }

    [Required(ErrorMessage = "Order is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Order must be a non-negative number")]
    public int Order { get; init; }
}

// Sprint Submission DTOs with Validation
public record SprintSubmissionRequestDto
{
    [Range(0, 1000, ErrorMessage = "Story points completed must be between 0 and 1000")]
    public int StoryPointsCompleted { get; init; }

    [Range(0, 1000, ErrorMessage = "Story points planned must be between 0 and 1000")]
    public int StoryPointsPlanned { get; init; }

    [Range(0, 1000, ErrorMessage = "Hours worked must be between 0 and 1000")]
public decimal HoursWorked { get; init; }

    [MaxLength(100, ErrorMessage = "Cannot have more than 100 user stories")]
    public List<UserStoryRequestDto>? UserStories { get; init; }

    [MaxLength(100, ErrorMessage = "Cannot have more than 100 features")]
    public List<FeatureRequestDto>? FeaturesDelivered { get; init; }

 [MaxLength(50, ErrorMessage = "Cannot have more than 50 impediments")]
    public List<ImpedimentRequestDto>? Impediments { get; init; }

    [MaxLength(50, ErrorMessage = "Cannot have more than 50 appreciations")]
    public List<AppreciationRequestDto>? Appreciations { get; init; }

    [StringLength(5000, ErrorMessage = "Achievements cannot exceed 5000 characters")]
    public string? Achievements { get; init; }

    [StringLength(5000, ErrorMessage = "Learnings cannot exceed 5000 characters")]
    public string? Learnings { get; init; }

    [StringLength(5000, ErrorMessage = "Next sprint goals cannot exceed 5000 characters")]
    public string? NextSprintGoals { get; init; }

    [StringLength(5000, ErrorMessage = "Additional notes cannot exceed 5000 characters")]
    public string? AdditionalNotes { get; init; }
}

public record UserStoryRequestDto
{
    [Required(ErrorMessage = "Story ID is required")]
    [StringLength(50, ErrorMessage = "Story ID cannot exceed 50 characters")]
    public string StoryId { get; init; } = null!;

    [Required(ErrorMessage = "Title is required")]
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string Title { get; init; } = null!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; init; }

    [Range(0, 100, ErrorMessage = "Story points must be between 0 and 100")]
  public int StoryPoints { get; init; }

    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    public string Status { get; init; } = "Completed";

    [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters")]
    public string? Remarks { get; init; }
}

public record FeatureRequestDto
{
    [Required(ErrorMessage = "Feature name is required")]
    [StringLength(500, ErrorMessage = "Feature name cannot exceed 500 characters")]
    public string FeatureName { get; init; } = null!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
 public string? Description { get; init; }

    [StringLength(200, ErrorMessage = "Module cannot exceed 200 characters")]
    public string? Module { get; init; }

    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    public string Status { get; init; } = "Delivered";
}

public record ImpedimentRequestDto
{
    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string Description { get; init; } = null!;

    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
    public string Category { get; init; } = "Technical";

    [StringLength(50, ErrorMessage = "Impact cannot exceed 50 characters")]
    public string Impact { get; init; } = "Medium";

  [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    public string Status { get; init; } = "Open";

    [StringLength(2000, ErrorMessage = "Resolution cannot exceed 2000 characters")]
    public string? Resolution { get; init; }

    public DateTime? ReportedDate { get; init; }
}

public record AppreciationRequestDto
{
    public string? AppreciatedUserId { get; init; }

    [Required(ErrorMessage = "Appreciated user name is required")]
    [StringLength(200, ErrorMessage = "Appreciated user name cannot exceed 200 characters")]
    public string AppreciatedUserName { get; init; } = null!;

    [Required(ErrorMessage = "Reason is required")]
    [StringLength(2000, ErrorMessage = "Reason cannot exceed 2000 characters")]
    public string Reason { get; init; } = null!;

    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
    public string Category { get; init; } = "Teamwork";
}

public record UpdateUserRequestDto
{
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
    public string? FirstName { get; init; }

    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
    public string? LastName { get; init; }

    [Url(ErrorMessage = "Avatar must be a valid URL")]
    [StringLength(2000, ErrorMessage = "Avatar URL cannot exceed 2000 characters")]
    public string? Avatar { get; init; }

    public UserRole? Role { get; init; }
}

/// <summary>
/// Request to update team members for a project
/// </summary>
public record UpdateTeamMembersRequest
{
    [Required(ErrorMessage = "Member IDs are required")]
    public List<string> MemberIds { get; init; } = new();
}

/// <summary>
/// Custom date validators for cross-field validation
/// </summary>
public static class DateValidators
{
    public static ValidationResult? ValidateTargetEndDate(DateTime? targetEndDate, ValidationContext context)
    {
        if (targetEndDate == null) return ValidationResult.Success;

        var instance = context.ObjectInstance;
  var startDateProperty = instance.GetType().GetProperty("StartDate");
        var startDate = startDateProperty?.GetValue(instance) as DateTime?;

        if (startDate.HasValue && targetEndDate < startDate)
   {
            return new ValidationResult("Target end date must be after start date");
        }

        return ValidationResult.Success;
    }

    public static ValidationResult? ValidateSprintEndDate(DateTime endDate, ValidationContext context)
    {
        var instance = context.ObjectInstance;
        var startDateProperty = instance.GetType().GetProperty("StartDate");
        var startDate = startDateProperty?.GetValue(instance) as DateTime?;

        if (startDate.HasValue && endDate <= startDate)
  {
  return new ValidationResult("End date must be after start date");
        }

        if (endDate < DateTime.UtcNow.Date)
      {
   return new ValidationResult("End date cannot be in the past");
 }

        return ValidationResult.Success;
    }
}
