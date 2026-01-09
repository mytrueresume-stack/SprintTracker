using MongoDB.Driver;
using SprintTracker.Api.Data;
using SprintTracker.Api.Exceptions;
using SprintTracker.Api.Models;
using SprintTracker.Api.Models.DTOs;
using TaskStatus = SprintTracker.Api.Models.TaskStatus;

namespace SprintTracker.Api.Services;

public interface ITaskService
{
    Task<TaskDto> CreateTaskAsync(CreateTaskRequest request, string userId);
    Task<TaskDto?> GetTaskByIdAsync(string taskId);
    Task<PaginatedResponse<TaskDto>> GetTasksAsync(TaskFilterOptions filter);
    Task<List<TaskDto>> GetBacklogTasksAsync(string projectId);
    Task<List<TaskDto>> GetSprintTasksAsync(string sprintId);
    Task<TaskDto?> UpdateTaskAsync(string taskId, UpdateTaskRequest request, string userId);
    Task<TaskDto?> UpdateTaskStatusAsync(string taskId, TaskStatus status, string userId);
    Task<bool> MoveTaskAsync(string taskId, MoveTaskRequest request, string userId);
    Task<bool> LogTimeAsync(string taskId, LogTimeRequest request, string userId);
    Task<bool> DeleteTaskAsync(string taskId, string userId);
    Task<List<TaskDto>> GetMyTasksAsync(string userId);
}

public class TaskFilterOptions
{
    public string? ProjectId { get; set; }
    public string? SprintId { get; set; }
    public string? AssigneeId { get; set; }
    public TaskStatus? Status { get; set; }
    public TaskType? Type { get; set; }
    public TaskPriority? Priority { get; set; }
    public string? SearchTerm { get; set; }
 public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class TaskService : ITaskService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<TaskService> _logger;

    public TaskService(MongoDbContext context, ILogger<TaskService> logger)
    {
    _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Check if user has permission to modify a task
 /// Task creator, assignee, project owner, managers, and admins can modify tasks
    /// </summary>
    private async Task<(bool HasPermission, User? CurrentUser, Project? Project)> CheckTaskPermissionAsync(
        SprintTask task, string userId, bool allowAssignee = true)
    {
   var currentUser = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (currentUser == null)
    return (false, null, null);

        var project = await _context.Projects.Find(p => p.Id == task.ProjectId).FirstOrDefaultAsync();
   if (project == null)
         return (false, currentUser, null);

     // Admins have full access
   if (currentUser.Role == UserRole.Admin)
            return (true, currentUser, project);

      // Project owner has full access
        if (project.OwnerId == userId)
         return (true, currentUser, project);

      // Task reporter (creator) has access
      if (task.ReporterId == userId)
      return (true, currentUser, project);

// Assignee can update their own tasks if allowed
        if (allowAssignee && task.AssigneeId == userId)
            return (true, currentUser, project);

        // Managers who are team members can manage tasks
        if (currentUser.Role == UserRole.Manager && project.TeamMemberIds.Contains(userId))
            return (true, currentUser, project);

        // Developers who are team members can update tasks (not delete)
        if (currentUser.Role == UserRole.Developer && project.TeamMemberIds.Contains(userId))
            return (true, currentUser, project);

      return (false, currentUser, project);
    }

    /// <summary>
    /// Check if user can delete a task (more restrictive than update)
    /// </summary>
    private async Task<(bool HasPermission, User? CurrentUser)> CheckTaskDeletePermissionAsync(
        SprintTask task, string userId)
    {
        var currentUser = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (currentUser == null)
    return (false, null);

        var project = await _context.Projects.Find(p => p.Id == task.ProjectId).FirstOrDefaultAsync();
        if (project == null)
       return (false, currentUser);

        // Admins have full access
     if (currentUser.Role == UserRole.Admin)
            return (true, currentUser);

        // Project owner can delete
        if (project.OwnerId == userId)
   return (true, currentUser);

        // Task reporter (creator) can delete
        if (task.ReporterId == userId)
            return (true, currentUser);

        // Managers who are team members can delete
        if (currentUser.Role == UserRole.Manager && project.TeamMemberIds.Contains(userId))
            return (true, currentUser);

      return (false, currentUser);
 }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest request, string userId)
    {
        try
        {
            // Get project to generate task key
    var project = await _context.Projects.Find(p => p.Id == request.ProjectId).FirstOrDefaultAsync();
        if (project == null)
        {
          throw new NotFoundException("Project", request.ProjectId);
        }

            // Verify user is a team member or has elevated permissions
            var currentUser = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
    if (currentUser == null)
            {
  throw new UnauthorizedAccessException("User not found");
  }

        var canCreate = currentUser.Role == UserRole.Admin ||
    project.OwnerId == userId ||
  project.TeamMemberIds.Contains(userId);

if (!canCreate)
            {
throw new ForbiddenException("You must be a team member to create tasks in this project.");
            }

            // Get next task number for this project
            var lastTask = await _context.Tasks
        .Find(t => t.ProjectId == request.ProjectId)
     .SortByDescending(t => t.CreatedAt)
       .FirstOrDefaultAsync();

            var taskNumber = 1;
        if (lastTask != null)
   {
            var parts = lastTask.TaskKey.Split('-');
         if (parts.Length == 2 && int.TryParse(parts[1], out var num))
       taskNumber = num + 1;
    }

            var task = new SprintTask
            {
     TaskKey = $"{project.Key}-{taskNumber}",
      ProjectId = request.ProjectId,
       SprintId = request.SprintId,
    ParentTaskId = request.ParentTaskId,
      Title = request.Title.Trim(),
       Description = request.Description?.Trim(),
      Type = request.Type,
                Priority = request.Priority,
     StoryPoints = request.StoryPoints,
           EstimatedHours = request.EstimatedHours,
         RemainingHours = request.EstimatedHours,
                AssigneeId = request.AssigneeId,
    ReporterId = userId,
          Labels = request.Labels ?? new List<string>(),
        DueDate = request.DueDate,
      CreatedAt = DateTime.UtcNow,
  UpdatedAt = DateTime.UtcNow
            };

            if (request.AcceptanceCriteria != null)
         {
                task.AcceptanceCriteria = request.AcceptanceCriteria
   .Select(ac => new AcceptanceCriterion { Description = ac.Trim() })
        .ToList();
 }

   // Set order for backlog/sprint
  var existingTaskCount = await _context.Tasks
            .CountDocumentsAsync(t => t.SprintId == request.SprintId && t.ProjectId == request.ProjectId);
    task.Order = (int)existingTaskCount;

     await _context.Tasks.InsertOneAsync(task);

 // Log activity
            await LogActivityAsync("Task", task.Id, "Created", userId, new List<FieldChange>());

            _logger.LogInformation("Task created: {TaskKey} in project {ProjectId} by user {UserId} (Role: {Role})", 
   task.TaskKey, request.ProjectId, userId, currentUser.Role);
            return await MapToTaskDtoAsync(task);
      }
        catch (NotFoundException)
  {
         throw;
        }
        catch (ForbiddenException)
        {
    throw;
        }
        catch (MongoException ex)
      {
            _logger.LogError(ex, "Database error creating task");
   throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<TaskDto?> GetTaskByIdAsync(string taskId)
    {
        try
        {
 var task = await _context.Tasks.Find(t => t.Id == taskId).FirstOrDefaultAsync();
      return task != null ? await MapToTaskDtoAsync(task) : null;
        }
     catch (MongoException ex)
        {
  _logger.LogError(ex, "Database error fetching task {TaskId}", taskId);
          throw new ServiceUnavailableException("Database", ex);
     }
    }

    public async Task<PaginatedResponse<TaskDto>> GetTasksAsync(TaskFilterOptions filter)
    {
   try
    {
      var filterBuilder = Builders<SprintTask>.Filter;
            var filters = new List<FilterDefinition<SprintTask>>();

     if (!string.IsNullOrEmpty(filter.ProjectId))
         filters.Add(filterBuilder.Eq(t => t.ProjectId, filter.ProjectId));
        if (!string.IsNullOrEmpty(filter.SprintId))
                filters.Add(filterBuilder.Eq(t => t.SprintId, filter.SprintId));
     if (!string.IsNullOrEmpty(filter.AssigneeId))
          filters.Add(filterBuilder.Eq(t => t.AssigneeId, filter.AssigneeId));
if (filter.Status.HasValue)
        filters.Add(filterBuilder.Eq(t => t.Status, filter.Status.Value));
    if (filter.Type.HasValue)
    filters.Add(filterBuilder.Eq(t => t.Type, filter.Type.Value));
     if (filter.Priority.HasValue)
                filters.Add(filterBuilder.Eq(t => t.Priority, filter.Priority.Value));
            if (!string.IsNullOrEmpty(filter.SearchTerm))
      {
              var escapedSearch = System.Text.RegularExpressions.Regex.Escape(filter.SearchTerm);
    filters.Add(filterBuilder.Or(
           filterBuilder.Regex(t => t.Title, new MongoDB.Bson.BsonRegularExpression(escapedSearch, "i")),
   filterBuilder.Regex(t => t.TaskKey, new MongoDB.Bson.BsonRegularExpression(escapedSearch, "i"))
     ));
          }

         var combinedFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;
        var totalCount = await _context.Tasks.CountDocumentsAsync(combinedFilter);

        var tasks = await _context.Tasks
     .Find(combinedFilter)
       .SortBy(t => t.Order)
            .Skip((filter.Page - 1) * filter.PageSize)
  .Limit(filter.PageSize)
     .ToListAsync();

            var taskDtos = new List<TaskDto>();
  foreach (var task in tasks)
            {
      taskDtos.Add(await MapToTaskDtoAsync(task));
      }

       return new PaginatedResponse<TaskDto>(
          taskDtos,
          (int)totalCount,
        filter.Page,
filter.PageSize,
   (int)Math.Ceiling((double)totalCount / filter.PageSize)
      );
        }
        catch (MongoException ex)
        {
 _logger.LogError(ex, "Database error fetching tasks");
            throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<List<TaskDto>> GetBacklogTasksAsync(string projectId)
  {
        try
        {
var tasks = await _context.Tasks
           .Find(t => t.ProjectId == projectId && t.SprintId == null && t.ParentTaskId == null)
                .SortBy(t => t.Order)
.ToListAsync();

            var taskDtos = new List<TaskDto>();
            foreach (var task in tasks)
    {
         taskDtos.Add(await MapToTaskDtoAsync(task));
     }
            return taskDtos;
  }
        catch (MongoException ex)
 {
 _logger.LogError(ex, "Database error fetching backlog tasks for project {ProjectId}", projectId);
            throw new ServiceUnavailableException("Database", ex);
      }
    }

    public async Task<List<TaskDto>> GetSprintTasksAsync(string sprintId)
    {
    try
        {
     var tasks = await _context.Tasks
           .Find(t => t.SprintId == sprintId && t.ParentTaskId == null)
              .SortBy(t => t.Order)
             .ToListAsync();

          var taskDtos = new List<TaskDto>();
  foreach (var task in tasks)
            {
             taskDtos.Add(await MapToTaskDtoAsync(task));
   }
 return taskDtos;
        }
 catch (MongoException ex)
        {
            _logger.LogError(ex, "Database error fetching tasks for sprint {SprintId}", sprintId);
            throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<TaskDto?> UpdateTaskAsync(string taskId, UpdateTaskRequest request, string userId)
    {
      try
        {
    var task = await _context.Tasks.Find(t => t.Id == taskId).FirstOrDefaultAsync();
 if (task == null) return null;

     // Check permission
         var (hasPermission, currentUser, _) = await CheckTaskPermissionAsync(task, userId);
            if (!hasPermission)
            {
  throw new ForbiddenException("You don't have permission to update this task.");
            }

            var changes = new List<FieldChange>();
            var updateBuilder = Builders<SprintTask>.Update.Set(t => t.UpdatedAt, DateTime.UtcNow);

     if (!string.IsNullOrWhiteSpace(request.Title) && request.Title.Trim() != task.Title)
          {
    changes.Add(new FieldChange { FieldName = "Title", OldValue = task.Title, NewValue = request.Title.Trim() });
     updateBuilder = updateBuilder.Set(t => t.Title, request.Title.Trim());
}
      if (request.Description != null)
           updateBuilder = updateBuilder.Set(t => t.Description, request.Description.Trim());
 if (request.Type.HasValue && request.Type != task.Type)
            {
          changes.Add(new FieldChange { FieldName = "Type", OldValue = task.Type.ToString(), NewValue = request.Type.ToString() });
      updateBuilder = updateBuilder.Set(t => t.Type, request.Type.Value);
      }
            if (request.Status.HasValue && request.Status != task.Status)
     {
      changes.Add(new FieldChange { FieldName = "Status", OldValue = task.Status.ToString(), NewValue = request.Status.ToString() });
updateBuilder = updateBuilder.Set(t => t.Status, request.Status.Value);
                if (request.Status == TaskStatus.Done)
    updateBuilder = updateBuilder.Set(t => t.CompletedAt, DateTime.UtcNow);
        }
     if (request.Priority.HasValue && request.Priority != task.Priority)
         {
      changes.Add(new FieldChange { FieldName = "Priority", OldValue = task.Priority.ToString(), NewValue = request.Priority.ToString() });
              updateBuilder = updateBuilder.Set(t => t.Priority, request.Priority.Value);
    }
     if (request.StoryPoints.HasValue)
     updateBuilder = updateBuilder.Set(t => t.StoryPoints, request.StoryPoints.Value);
            if (request.EstimatedHours.HasValue)
    updateBuilder = updateBuilder.Set(t => t.EstimatedHours, request.EstimatedHours.Value);
   if (request.RemainingHours.HasValue)
     updateBuilder = updateBuilder.Set(t => t.RemainingHours, request.RemainingHours.Value);
       if (request.AssigneeId != null)
       {
     changes.Add(new FieldChange { FieldName = "Assignee", OldValue = task.AssigneeId, NewValue = request.AssigneeId });
    updateBuilder = updateBuilder.Set(t => t.AssigneeId, request.AssigneeId);
            }
            if (request.SprintId != null)
    {
     changes.Add(new FieldChange { FieldName = "Sprint", OldValue = task.SprintId, NewValue = request.SprintId });
                updateBuilder = updateBuilder.Set(t => t.SprintId, request.SprintId);
            }
   if (request.Labels != null)
                updateBuilder = updateBuilder.Set(t => t.Labels, request.Labels);
       if (request.DueDate.HasValue)
           updateBuilder = updateBuilder.Set(t => t.DueDate, request.DueDate.Value);
            if (request.Order.HasValue)
  updateBuilder = updateBuilder.Set(t => t.Order, request.Order.Value);

      await _context.Tasks.UpdateOneAsync(t => t.Id == taskId, updateBuilder);

            if (changes.Any())
  {
      await LogActivityAsync("Task", taskId, "Updated", userId, changes);
      _logger.LogInformation("Task {TaskId} updated by user {UserId} (Role: {Role})", 
         taskId, userId, currentUser?.Role);
          }

         return await GetTaskByIdAsync(taskId);
        }
      catch (ForbiddenException)
        {
       throw;
   }
        catch (MongoException ex)
        {
     _logger.LogError(ex, "Database error updating task {TaskId}", taskId);
      throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<TaskDto?> UpdateTaskStatusAsync(string taskId, TaskStatus status, string userId)
    {
  return await UpdateTaskAsync(taskId, new UpdateTaskRequest(
          Title: null,
   Description: null,
            Type: null,
            Status: status,
    Priority: null,
  StoryPoints: null,
            EstimatedHours: null,
            RemainingHours: null,
AssigneeId: null,
         SprintId: null,
            Labels: null,
     DueDate: null,
            Order: null
   ), userId);
    }

    public async Task<bool> MoveTaskAsync(string taskId, MoveTaskRequest request, string userId)
    {
        try
        {
    var task = await _context.Tasks.Find(t => t.Id == taskId).FirstOrDefaultAsync();
      if (task == null) return false;

      // Check permission
          var (hasPermission, currentUser, _) = await CheckTaskPermissionAsync(task, userId);
          if (!hasPermission)
  {
        throw new ForbiddenException("You don't have permission to move this task.");
            }

            var update = Builders<SprintTask>.Update
      .Set(t => t.SprintId, request.SprintId)
         .Set(t => t.Order, request.Order)
   .Set(t => t.UpdatedAt, DateTime.UtcNow);

var result = await _context.Tasks.UpdateOneAsync(t => t.Id == taskId, update);

            await LogActivityAsync("Task", taskId, "Moved", userId, new List<FieldChange>
  {
                new() { FieldName = "SprintId", OldValue = task.SprintId, NewValue = request.SprintId }
        });

      _logger.LogInformation("Task {TaskId} moved to sprint {SprintId} by user {UserId} (Role: {Role})", 
  taskId, request.SprintId ?? "backlog", userId, currentUser?.Role);
       return result.ModifiedCount > 0;
        }
        catch (ForbiddenException)
   {
   throw;
}
      catch (MongoException ex)
        {
            _logger.LogError(ex, "Database error moving task {TaskId}", taskId);
            throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<bool> LogTimeAsync(string taskId, LogTimeRequest request, string userId)
    {
     try
        {
      var task = await _context.Tasks.Find(t => t.Id == taskId).FirstOrDefaultAsync();
  if (task == null) return false;

            // Any team member can log time (we'll check permission through project membership)
            var project = await _context.Projects.Find(p => p.Id == task.ProjectId).FirstOrDefaultAsync();
      var currentUser = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (project == null || currentUser == null)
            return false;

        var canLogTime = currentUser.Role == UserRole.Admin ||
               project.OwnerId == userId ||
           project.TeamMemberIds.Contains(userId);

            if (!canLogTime)
        {
  throw new ForbiddenException("You must be a team member to log time on this task.");
            }

 var newLoggedHours = task.LoggedHours + request.Hours;
  var newRemainingHours = task.RemainingHours.HasValue
          ? Math.Max(0, task.RemainingHours.Value - request.Hours)
                : (decimal?)null;

        var update = Builders<SprintTask>.Update
    .Set(t => t.LoggedHours, newLoggedHours)
        .Set(t => t.RemainingHours, newRemainingHours)
        .Set(t => t.UpdatedAt, DateTime.UtcNow);

            await _context.Tasks.UpdateOneAsync(t => t.Id == taskId, update);

    await LogActivityAsync("Task", taskId, "TimeLogged", userId, new List<FieldChange>
       {
      new() { FieldName = "LoggedHours", OldValue = task.LoggedHours.ToString(), NewValue = newLoggedHours.ToString() }
            });

  _logger.LogInformation("Time logged: {Hours}h on task {TaskId} by user {UserId}", 
       request.Hours, taskId, userId);
  return true;
        }
        catch (ForbiddenException)
        {
         throw;
        }
        catch (MongoException ex)
        {
     _logger.LogError(ex, "Database error logging time for task {TaskId}", taskId);
    throw new ServiceUnavailableException("Database", ex);
    }
    }

    public async Task<bool> DeleteTaskAsync(string taskId, string userId)
    {
        try
        {
    var task = await _context.Tasks.Find(t => t.Id == taskId).FirstOrDefaultAsync();
  if (task == null) return false;

            // Check delete permission (more restrictive)
            var (hasPermission, currentUser) = await CheckTaskDeletePermissionAsync(task, userId);
          if (!hasPermission)
          {
  throw new ForbiddenException("You don't have permission to delete this task. Only the task creator, project owner, managers, or administrators can delete tasks.");
          }

     // Also delete subtasks
 var subtaskCount = await _context.Tasks.DeleteManyAsync(t => t.ParentTaskId == taskId);
            var result = await _context.Tasks.DeleteOneAsync(t => t.Id == taskId);
   
         if (result.DeletedCount > 0)
            {
      _logger.LogInformation("Task {TaskKey} deleted by user {UserId} (Role: {Role}). {SubtaskCount} subtasks also deleted.", 
       task.TaskKey, userId, currentUser?.Role, subtaskCount.DeletedCount);
       }
  
        return result.DeletedCount > 0;
        }
        catch (ForbiddenException)
        {
  throw;
        }
    catch (MongoException ex)
        {
            _logger.LogError(ex, "Database error deleting task {TaskId}", taskId);
          throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<List<TaskDto>> GetMyTasksAsync(string userId)
    {
      try
        {
     var tasks = await _context.Tasks
      .Find(t => t.AssigneeId == userId && t.Status != TaskStatus.Done)
     .SortBy(t => t.Priority)
      .ThenByDescending(t => t.DueDate)
   .Limit(20)
         .ToListAsync();

            var taskDtos = new List<TaskDto>();
   foreach (var task in tasks)
       {
                taskDtos.Add(await MapToTaskDtoAsync(task));
          }
            return taskDtos;
   }
        catch (MongoException ex)
     {
         _logger.LogError(ex, "Database error fetching tasks for user {UserId}", userId);
            throw new ServiceUnavailableException("Database", ex);
        }
    }

    private async Task<TaskDto> MapToTaskDtoAsync(SprintTask task)
    {
  User? assignee = null;
        if (!string.IsNullOrEmpty(task.AssigneeId))
          assignee = await _context.Users.Find(u => u.Id == task.AssigneeId).FirstOrDefaultAsync();

        var reporter = await _context.Users.Find(u => u.Id == task.ReporterId).FirstOrDefaultAsync();

        // Get subtasks
        var subtasks = await _context.Tasks.Find(t => t.ParentTaskId == task.Id).ToListAsync();
        List<TaskDto>? subtaskDtos = null;
  if (subtasks.Any())
        {
            subtaskDtos = new List<TaskDto>();
            foreach (var subtask in subtasks)
     {
         subtaskDtos.Add(await MapToTaskDtoAsync(subtask));
       }
      }

        return new TaskDto(
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
            subtaskDtos
        );
    }

    private async Task LogActivityAsync(string entityType, string entityId, string action, string userId, List<FieldChange> changes)
    {
        try
        {
 var log = new ActivityLog
      {
   EntityType = entityType,
              EntityId = entityId,
           Action = action,
        UserId = userId,
       Changes = changes,
  Timestamp = DateTime.UtcNow
      };
            await _context.ActivityLogs.InsertOneAsync(log);
        }
     catch (MongoException ex)
 {
            // Don't throw - activity logging is supplementary
         _logger.LogWarning(ex, "Failed to log activity for {EntityType} {EntityId}", entityType, entityId);
  }
    }
}
