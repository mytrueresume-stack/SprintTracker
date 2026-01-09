using MongoDB.Driver;
using SprintTracker.Api.Data;
using SprintTracker.Api.Exceptions;
using SprintTracker.Api.Models;
using SprintTracker.Api.Models.DTOs;
using TaskStatus = SprintTracker.Api.Models.TaskStatus;

namespace SprintTracker.Api.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetDashboardStatsAsync(string userId);
    Task<List<ActivityLog>> GetRecentActivityAsync(string userId, int count = 20);
}

public class DashboardService : IDashboardService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<DashboardService> _logger;

  public DashboardService(MongoDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
  }

    public async Task<DashboardStats> GetDashboardStatsAsync(string userId)
  {
        try
   {
      // Get user's projects
       var projects = await _context.Projects
   .Find(p => (p.OwnerId == userId || p.TeamMemberIds.Contains(userId)) && p.Status != ProjectStatus.Archived)
      .ToListAsync();

var projectIds = projects.Select(p => p.Id).ToList();

   // Get active sprints
  var activeSprints = await _context.Sprints
    .Find(s => projectIds.Contains(s.ProjectId) && s.Status == SprintStatus.Active)
.ToListAsync();

      // Get all tasks from user's projects
            var allTasks = await _context.Tasks
     .Find(t => projectIds.Contains(t.ProjectId))
 .ToListAsync();

            // Get my tasks
         var myTasks = await _context.Tasks
  .Find(t => t.AssigneeId == userId && t.Status != TaskStatus.Done)
 .SortByDescending(t => t.Priority)
     .Limit(10)
     .ToListAsync();

// Build recent sprints summary
     var recentSprints = new List<SprintSummary>();
       foreach (var sprint in activeSprints.Take(5))
    {
      var project = projects.FirstOrDefault(p => p.Id == sprint.ProjectId);
      var sprintTasks = allTasks.Where(t => t.SprintId == sprint.Id).ToList();
      var totalPoints = sprintTasks.Where(t => t.StoryPoints.HasValue).Sum(t => t.StoryPoints!.Value);
      var completedPoints = sprintTasks
        .Where(t => t.Status == TaskStatus.Done && t.StoryPoints.HasValue)
        .Sum(t => t.StoryPoints!.Value);

      // Prefer developer-submitted sprint submissions to compute completion percentage.
      // If no SUBMITTED submissions exist, fall back to DRAFT submissions (so managers can see in-progress reports).
      // When submissions exist but have zero planned points, fall back to task-based totals.
      var submittedSubs = await _context.SprintSubmissions
        .Find(ss => ss.SprintId == sprint.Id && ss.Status == SubmissionStatus.Submitted)
        .ToListAsync();

      var usedSubs = submittedSubs;

      if (usedSubs == null || !usedSubs.Any())
      {
        // Try drafts as a fallback so in-progress reports are visible
        var draftSubs = await _context.SprintSubmissions
          .Find(ss => ss.SprintId == sprint.Id && ss.Status == SubmissionStatus.Draft)
          .ToListAsync();
        if (draftSubs != null && draftSubs.Any())
        {
          usedSubs = draftSubs;
          _logger.LogDebug("Using draft submissions for sprint {SprintId} as no submitted submissions found", sprint.Id);
        }
      }

      decimal completionPct;
      if (usedSubs != null && usedSubs.Any())
      {
        var plannedFromSubs = usedSubs.Sum(s => s.StoryPointsPlanned);
        var completedFromSubs = usedSubs.Sum(s => s.StoryPointsCompleted);

        if (plannedFromSubs > 0)
        {
          completionPct = (decimal)completedFromSubs / plannedFromSubs * 100;
          _logger.LogDebug("Using submissions-based completion for sprint {SprintId}: {Completed}/{Planned}", sprint.Id, completedFromSubs, plannedFromSubs);
        }
        else if (totalPoints > 0)
        {
          // If no planned points in submissions but tasks have story points, use tasks as denominator
          completionPct = (decimal)completedFromSubs / totalPoints * 100;
          _logger.LogDebug("Submissions have no planned points; using task totals for sprint {SprintId}", sprint.Id);
        }
        else if (completedFromSubs > 0)
        {
          // Defensive fallback: submissions reported completed points but no planned/task points exist. Treat as fully completed.
          completionPct = 100;
          _logger.LogDebug("Submissions reported completed points but no planned/task totals; treating sprint {SprintId} as 100% complete", sprint.Id);
        }
        else
        {
          completionPct = 0;
        }
      }
      else
      {
        completionPct = totalPoints > 0 ? (decimal)completedPoints / totalPoints * 100 : 0;
      }

      var daysRemaining = (sprint.EndDate - DateTime.UtcNow).Days;

      var sourceStr = (usedSubs != null && usedSubs.Any()) ? (usedSubs.All(s => s.Status == SubmissionStatus.Submitted) ? "Submissions:Submitted" : "Submissions:Draft") : "Tasks";
      recentSprints.Add(new SprintSummary(
        sprint.Id,
        sprint.Name,
        project?.Name ?? "Unknown",
        sprint.Status,
        sprint.EndDate,
        Math.Round(completionPct, 1),
        Math.Max(0, daysRemaining),
        // completion source: Submitted / Draft / Tasks
        sourceStr
      ));

      _logger.LogInformation("SprintSummary computed for {SprintId}: Completion={CompletionPct}, Source={Source}", sprint.Id, Math.Round(completionPct, 1), sourceStr);
    }

     // Map my tasks to DTOs
          var myTaskDtos = new List<TaskDto>();
            foreach (var task in myTasks)
  {
  var reporter = await _context.Users.Find(u => u.Id == task.ReporterId).FirstOrDefaultAsync();
   var assignee = !string.IsNullOrEmpty(task.AssigneeId)
? await _context.Users.Find(u => u.Id == task.AssigneeId).FirstOrDefaultAsync()
  : null;

       myTaskDtos.Add(new TaskDto(
 task.Id,
         task.TaskKey,
      task.ProjectId,
  task.SprintId,
           task.ParentTaskId,
  task.Title,
         task.Description,
    task.Type,
            task.Status,
  task.Priority,
task.StoryPoints,
       task.EstimatedHours,
  task.LoggedHours,
    task.RemainingHours,
   assignee != null ? new UserDto(assignee.Id, assignee.Email, assignee.FirstName, assignee.LastName, assignee.FullName, assignee.Role, assignee.Avatar, assignee.IsActive) : null,
       reporter != null ? new UserDto(reporter.Id, reporter.Email, reporter.FirstName, reporter.LastName, reporter.FullName, reporter.Role, reporter.Avatar, reporter.IsActive) : null!,
           task.Labels,
     task.AcceptanceCriteria,
       task.BlockedByTaskIds,
      task.DueDate,
  task.Order,
                 task.CreatedAt,
     task.CompletedAt,
     null
   ));
            }

return new DashboardStats(
  projects.Count,
   activeSprints.Count,
 allTasks.Count,
       allTasks.Count(t => t.Status == TaskStatus.InProgress),
  allTasks.Count(t => t.Status == TaskStatus.Done),
    allTasks.Count(t => t.Status == TaskStatus.Blocked),
      recentSprints,
     myTaskDtos
    );
        }
    catch (MongoException ex)
        {
    _logger.LogError(ex, "Database error fetching dashboard stats for user {UserId}", userId);
       throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<List<ActivityLog>> GetRecentActivityAsync(string userId, int count = 20)
    {
        try
        {
 // Get user's projects
   var projects = await _context.Projects
     .Find(p => p.OwnerId == userId || p.TeamMemberIds.Contains(userId))
         .ToListAsync();

          var projectIds = projects.Select(p => p.Id).ToList();

            // Get tasks from these projects
            var taskIds = await _context.Tasks
       .Find(t => projectIds.Contains(t.ProjectId))
.Project(t => t.Id)
.ToListAsync();

     // Get recent activity
 return await _context.ActivityLogs
    .Find(a => taskIds.Contains(a.EntityId) || a.UserId == userId)
 .SortByDescending(a => a.Timestamp)
.Limit(count)
  .ToListAsync();
        }
        catch (MongoException ex)
        {
 _logger.LogError(ex, "Database error fetching activity for user {UserId}", userId);
            throw new ServiceUnavailableException("Database", ex);
      }
    }
}
