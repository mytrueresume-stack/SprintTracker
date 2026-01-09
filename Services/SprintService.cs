using MongoDB.Driver;
using SprintTracker.Api.Data;
using SprintTracker.Api.Exceptions;
using SprintTracker.Api.Models;
using SprintTracker.Api.Models.DTOs;

namespace SprintTracker.Api.Services;

public interface ISprintService
{
    Task<SprintDto> CreateSprintAsync(CreateSprintRequest request, string userId);
    Task<SprintDto?> GetSprintByIdAsync(string sprintId);
    Task<PaginatedResponse<SprintDto>> GetSprintsByProjectAsync(string projectId, int page = 1, int pageSize = 10);
    Task<SprintDto?> GetActiveSprintAsync(string projectId);
Task<SprintDto?> UpdateSprintAsync(string sprintId, UpdateSprintRequest request, string userId);
    Task<SprintDto?> StartSprintAsync(string sprintId, string userId);
    Task<SprintDto?> CompleteSprintAsync(string sprintId, SprintRetrospective? retrospective, string userId);
    Task<bool> DeleteSprintAsync(string sprintId, string userId);
    Task<BurndownData> GetBurndownDataAsync(string sprintId);
    Task<VelocityData> GetVelocityDataAsync(string projectId);
}

public class SprintService : ISprintService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<SprintService> _logger;

    public SprintService(MongoDbContext context, ILogger<SprintService> logger)
    {
   _context = context;
  _logger = logger;
    }

    /// <summary>
    /// Check if user has permission to manage sprints (owner, team manager, or admin)
    /// </summary>
    private async Task<(bool HasPermission, User? CurrentUser, Project? Project)> CheckSprintPermissionAsync(
        string projectId, string userId)
    {
  var project = await _context.Projects.Find(p => p.Id == projectId).FirstOrDefaultAsync();
     if (project == null)
            return (false, null, null);

    var currentUser = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (currentUser == null)
 return (false, null, project);

        // Admins have full access
        if (currentUser.Role == UserRole.Admin)
        return (true, currentUser, project);

     // Project owner has access
        if (project.OwnerId == userId)
            return (true, currentUser, project);

        // Managers who are team members can manage sprints
  if (currentUser.Role == UserRole.Manager && project.TeamMemberIds.Contains(userId))
     return (true, currentUser, project);

        return (false, currentUser, project);
    }

    public async Task<SprintDto> CreateSprintAsync(CreateSprintRequest request, string userId)
    {
        try
        {
            // Verify project exists and user has permission
            var (hasPermission, currentUser, project) = await CheckSprintPermissionAsync(request.ProjectId, userId);
       
  if (project == null)
         {
              throw new NotFoundException("Project", request.ProjectId);
            }

   if (!hasPermission)
    {
     throw new ForbiddenException("You don't have permission to create sprints for this project. Only project owners, team managers, or administrators can create sprints.");
   }

   // Check if there's already an active sprint
       var activeSprint = await _context.Sprints
       .Find(s => s.ProjectId == request.ProjectId && s.Status == SprintStatus.Active)
       .FirstOrDefaultAsync();

      if (activeSprint != null)
      {
    _logger.LogWarning("Cannot create sprint: Project {ProjectId} already has an active sprint", request.ProjectId);
    // This is just a warning, not blocking creation
            }

// Validate dates
            if (request.EndDate <= request.StartDate)
    {
 throw new BusinessRuleViolationException("Sprint end date must be after start date", "INVALID_DATES");
            }

    // Get the next sprint number for this project
            var lastSprint = await _context.Sprints
                .Find(s => s.ProjectId == request.ProjectId)
      .SortByDescending(s => s.SprintNumber)
  .FirstOrDefaultAsync();

       var sprint = new Sprint
   {
        ProjectId = request.ProjectId,
          Name = request.Name.Trim(),
    Goal = request.Goal?.Trim(),
           SprintNumber = (lastSprint?.SprintNumber ?? 0) + 1,
  StartDate = request.StartDate,
    EndDate = request.EndDate,
      CreatedBy = userId,
        CreatedAt = DateTime.UtcNow,
  UpdatedAt = DateTime.UtcNow
            };

            await _context.Sprints.InsertOneAsync(sprint);
            _logger.LogInformation("Sprint created: {SprintName} (#{SprintNumber}) for project {ProjectId} by user {UserId} (Role: {Role})", 
     sprint.Name, sprint.SprintNumber, sprint.ProjectId, userId, currentUser?.Role);

      return await MapToSprintDtoAsync(sprint);
 }
catch (NotFoundException)
        {
  throw;
     }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (BusinessRuleViolationException)
        {
            throw;
        }
      catch (MongoException ex)
    {
         _logger.LogError(ex, "Database error creating sprint");
            throw new ServiceUnavailableException("Database", ex);
     }
    }

    public async Task<SprintDto?> GetSprintByIdAsync(string sprintId)
    {
        try
 {
         var sprint = await _context.Sprints.Find(s => s.Id == sprintId).FirstOrDefaultAsync();
            return sprint != null ? await MapToSprintDtoAsync(sprint) : null;
        }
        catch (MongoException ex)
        {
    _logger.LogError(ex, "Database error fetching sprint {SprintId}", sprintId);
 throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<PaginatedResponse<SprintDto>> GetSprintsByProjectAsync(string projectId, int page = 1, int pageSize = 10)
    {
        try
    {
    var filter = Builders<Sprint>.Filter.Eq(s => s.ProjectId, projectId);
   var totalCount = await _context.Sprints.CountDocumentsAsync(filter);

       var sprints = await _context.Sprints
         .Find(filter)
          .SortByDescending(s => s.SprintNumber)
    .Skip((page - 1) * pageSize)
     .Limit(pageSize)
   .ToListAsync();

         var sprintDtos = new List<SprintDto>();
            foreach (var sprint in sprints)
            {
           sprintDtos.Add(await MapToSprintDtoAsync(sprint));
            }

            return new PaginatedResponse<SprintDto>(
      sprintDtos,
 (int)totalCount,
   page,
         pageSize,
       (int)Math.Ceiling((double)totalCount / pageSize)
  );
        }
        catch (MongoException ex)
        {
_logger.LogError(ex, "Database error fetching sprints for project {ProjectId}", projectId);
        throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<SprintDto?> GetActiveSprintAsync(string projectId)
    {
      try
        {
            var sprint = await _context.Sprints
 .Find(s => s.ProjectId == projectId && s.Status == SprintStatus.Active)
 .FirstOrDefaultAsync();
            return sprint != null ? await MapToSprintDtoAsync(sprint) : null;
        }
        catch (MongoException ex)
    {
            _logger.LogError(ex, "Database error fetching active sprint for project {ProjectId}", projectId);
         throw new ServiceUnavailableException("Database", ex);
     }
    }

    public async Task<SprintDto?> UpdateSprintAsync(string sprintId, UpdateSprintRequest request, string userId)
    {
 try
    {
  var sprint = await _context.Sprints.Find(s => s.Id == sprintId).FirstOrDefaultAsync();
 if (sprint == null) return null;

    // Check permission
            var (hasPermission, currentUser, _) = await CheckSprintPermissionAsync(sprint.ProjectId, userId);
      if (!hasPermission)
  {
      throw new ForbiddenException("You don't have permission to update this sprint.");
         }

    // Cannot update completed sprints
            if (sprint.Status == SprintStatus.Completed)
   {
      throw new BusinessRuleViolationException("Cannot update a completed sprint", "SPRINT_COMPLETED");
        }

            var updateBuilder = Builders<Sprint>.Update.Set(s => s.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrWhiteSpace(request.Name))
       updateBuilder = updateBuilder.Set(s => s.Name, request.Name.Trim());
            if (request.Goal != null)
    updateBuilder = updateBuilder.Set(s => s.Goal, request.Goal.Trim());
   if (request.Status.HasValue)
            {
    // Validate status transitions
          if (!IsValidStatusTransition(sprint.Status, request.Status.Value))
  {
        throw new BusinessRuleViolationException(
       $"Cannot transition from {sprint.Status} to {request.Status.Value}", "INVALID_STATUS_TRANSITION");
        }
                updateBuilder = updateBuilder.Set(s => s.Status, request.Status.Value);
        }
    if (request.StartDate.HasValue)
       updateBuilder = updateBuilder.Set(s => s.StartDate, request.StartDate.Value);
    if (request.EndDate.HasValue)
updateBuilder = updateBuilder.Set(s => s.EndDate, request.EndDate.Value);
            if (request.Capacity != null)
          updateBuilder = updateBuilder.Set(s => s.Capacity, request.Capacity);

            await _context.Sprints.UpdateOneAsync(s => s.Id == sprintId, updateBuilder);
     _logger.LogInformation("Sprint {SprintId} updated by user {UserId} (Role: {Role})", 
       sprintId, userId, currentUser?.Role);

         return await GetSprintByIdAsync(sprintId);
 }
        catch (ForbiddenException)
        {
    throw;
        }
    catch (BusinessRuleViolationException)
        {
        throw;
  }
        catch (MongoException ex)
        {
    _logger.LogError(ex, "Database error updating sprint {SprintId}", sprintId);
          throw new ServiceUnavailableException("Database", ex);
   }
    }

    private static bool IsValidStatusTransition(SprintStatus current, SprintStatus target)
    {
        return (current, target) switch
        {
     (SprintStatus.Planning, SprintStatus.Active) => true,
            (SprintStatus.Planning, SprintStatus.Cancelled) => true,
      (SprintStatus.Active, SprintStatus.Completed) => true,
      (SprintStatus.Active, SprintStatus.Cancelled) => true,
            _ => current == target // Allow setting same status
        };
    }

    public async Task<SprintDto?> StartSprintAsync(string sprintId, string userId)
    {
        try
      {
   var sprint = await _context.Sprints.Find(s => s.Id == sprintId).FirstOrDefaultAsync();
            if (sprint == null) return null;

   // Check permission
            var (hasPermission, currentUser, _) = await CheckSprintPermissionAsync(sprint.ProjectId, userId);
            if (!hasPermission)
  {
                throw new ForbiddenException("You don't have permission to start this sprint. Only project owners, team managers, or administrators can start sprints.");
   }

            if (sprint.Status != SprintStatus.Planning)
         {
    _logger.LogWarning("Cannot start sprint {SprintId}: Current status is {Status}", sprintId, sprint.Status);
       throw new BusinessRuleViolationException($"Cannot start sprint: Current status is {sprint.Status}", "INVALID_STATUS");
 }

            // Check if there's already an active sprint for this project
  var existingActive = await _context.Sprints
       .Find(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active && s.Id != sprintId)
         .FirstOrDefaultAsync();

         if (existingActive != null)
         {
        throw new BusinessRuleViolationException(
              "Cannot start sprint: Another sprint is already active for this project", "ACTIVE_SPRINT_EXISTS");
            }

            // Calculate committed story points from tasks in this sprint
   var tasks = await _context.Tasks.Find(t => t.SprintId == sprintId).ToListAsync();
            var committedPoints = tasks.Where(t => t.StoryPoints.HasValue).Sum(t => t.StoryPoints!.Value);

       var update = Builders<Sprint>.Update
    .Set(s => s.Status, SprintStatus.Active)
           .Set(s => s.StartedAt, DateTime.UtcNow)
   .Set(s => s.Capacity.CommittedStoryPoints, committedPoints)
      .Set(s => s.UpdatedAt, DateTime.UtcNow);

       await _context.Sprints.UpdateOneAsync(s => s.Id == sprintId, update);

      // Record initial burndown metric
  await RecordSprintMetrics(sprintId);

         _logger.LogInformation("Sprint started: {SprintId} with {CommittedPoints} committed story points by user {UserId} (Role: {Role})", 
             sprintId, committedPoints, userId, currentUser?.Role);
          return await GetSprintByIdAsync(sprintId);
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (BusinessRuleViolationException)
        {
            throw;
        }
    catch (MongoException ex)
        {
         _logger.LogError(ex, "Database error starting sprint {SprintId}", sprintId);
            throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<SprintDto?> CompleteSprintAsync(string sprintId, SprintRetrospective? retrospective, string userId)
    {
        try
      {
            var sprint = await _context.Sprints.Find(s => s.Id == sprintId).FirstOrDefaultAsync();
            if (sprint == null) return null;

            // Check permission
    var (hasPermission, currentUser, _) = await CheckSprintPermissionAsync(sprint.ProjectId, userId);
            if (!hasPermission)
            {
   throw new ForbiddenException("You don't have permission to complete this sprint. Only project owners, team managers, or administrators can complete sprints.");
            }

      if (sprint.Status != SprintStatus.Active)
          {
           _logger.LogWarning("Cannot complete sprint {SprintId}: Current status is {Status}", sprintId, sprint.Status);
       throw new BusinessRuleViolationException($"Cannot complete sprint: Current status is {sprint.Status}", "INVALID_STATUS");
            }

       // Calculate actual velocity
     var completedTasks = await _context.Tasks
         .Find(t => t.SprintId == sprintId && t.Status == Models.TaskStatus.Done)
       .ToListAsync();
            var actualVelocity = completedTasks.Where(t => t.StoryPoints.HasValue).Sum(t => t.StoryPoints!.Value);

            var update = Builders<Sprint>.Update
      .Set(s => s.Status, SprintStatus.Completed)
                .Set(s => s.CompletedAt, DateTime.UtcNow)
                .Set(s => s.ActualVelocity, actualVelocity)
         .Set(s => s.UpdatedAt, DateTime.UtcNow);

            if (retrospective != null)
        update = update.Set(s => s.Retrospective, retrospective);

            await _context.Sprints.UpdateOneAsync(s => s.Id == sprintId, update);

            // Move incomplete tasks back to backlog
  var incompleteTaskUpdate = Builders<SprintTask>.Update
            .Set(t => t.SprintId, null)
     .Set(t => t.UpdatedAt, DateTime.UtcNow);

            var incompleteCount = await _context.Tasks.CountDocumentsAsync(
                t => t.SprintId == sprintId && t.Status != Models.TaskStatus.Done);

     await _context.Tasks.UpdateManyAsync(
             t => t.SprintId == sprintId && t.Status != Models.TaskStatus.Done,
     incompleteTaskUpdate);

      _logger.LogInformation(
        "Sprint completed: {SprintId} with velocity {Velocity}/{Committed}. {IncompleteCount} tasks moved to backlog. By user {UserId} (Role: {Role})",
         sprintId, actualVelocity, sprint.Capacity.CommittedStoryPoints, incompleteCount, userId, currentUser?.Role);

            return await GetSprintByIdAsync(sprintId);
        }
   catch (ForbiddenException)
   {
            throw;
        }
        catch (BusinessRuleViolationException)
        {
     throw;
        }
        catch (MongoException ex)
      {
        _logger.LogError(ex, "Database error completing sprint {SprintId}", sprintId);
  throw new ServiceUnavailableException("Database", ex);
     }
    }

    public async Task<bool> DeleteSprintAsync(string sprintId, string userId)
    {
        try
    {
            var sprint = await _context.Sprints.Find(s => s.Id == sprintId).FirstOrDefaultAsync();
    if (sprint == null) return false;

       // Check permission
            var (hasPermission, currentUser, _) = await CheckSprintPermissionAsync(sprint.ProjectId, userId);
       if (!hasPermission)
          {
       throw new ForbiddenException("You don't have permission to delete this sprint.");
            }

 if (sprint.Status == SprintStatus.Active)
  {
   _logger.LogWarning("Cannot delete active sprint {SprintId}", sprintId);
        throw new BusinessRuleViolationException("Cannot delete an active sprint. Complete or cancel it first.", "SPRINT_ACTIVE");
            }

            // Move tasks back to backlog
  var update = Builders<SprintTask>.Update
            .Set(t => t.SprintId, null)
      .Set(t => t.UpdatedAt, DateTime.UtcNow);

     await _context.Tasks.UpdateManyAsync(t => t.SprintId == sprintId, update);
          await _context.Sprints.DeleteOneAsync(s => s.Id == sprintId);

    _logger.LogInformation("Sprint deleted: {SprintId} by user {UserId} (Role: {Role})", 
 sprintId, userId, currentUser?.Role);
    return true;
        }
    catch (ForbiddenException)
        {
throw;
        }
    catch (BusinessRuleViolationException)
 {
            throw;
        }
        catch (MongoException ex)
        {
 _logger.LogError(ex, "Database error deleting sprint {SprintId}", sprintId);
            throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<BurndownData> GetBurndownDataAsync(string sprintId)
    {
        try
  {
            var sprint = await _context.Sprints.Find(s => s.Id == sprintId).FirstOrDefaultAsync();
            if (sprint == null)
return new BurndownData(sprintId, new List<BurndownPoint>(), new List<BurndownPoint>());

var metrics = await _context.Metrics
              .Find(m => m.SprintId == sprintId)
     .SortBy(m => m.Date)
       .ToListAsync();

     var totalDays = Math.Max(1, (sprint.EndDate - sprint.StartDate).Days);
            var totalPoints = sprint.Capacity.CommittedStoryPoints;

     var idealBurndown = new List<BurndownPoint>();
  var actualBurndown = new List<BurndownPoint>();

        // Generate ideal burndown
  for (int i = 0; i <= totalDays; i++)
        {
           var date = sprint.StartDate.AddDays(i);
        var idealPoints = totalPoints - (totalPoints * i / (decimal)totalDays);
        idealBurndown.Add(new BurndownPoint(date, Math.Round(idealPoints, 1)));
            }

      // Map actual burndown from metrics
            foreach (var metric in metrics)
        {
                actualBurndown.Add(new BurndownPoint(metric.Date, metric.RemainingStoryPoints));
            }

        return new BurndownData(sprintId, idealBurndown, actualBurndown);
        }
   catch (MongoException ex)
    {
    _logger.LogError(ex, "Database error fetching burndown data for sprint {SprintId}", sprintId);
   throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<VelocityData> GetVelocityDataAsync(string projectId)
    {
     try
        {
         var completedSprints = await _context.Sprints
    .Find(s => s.ProjectId == projectId && s.Status == SprintStatus.Completed)
  .SortByDescending(s => s.CompletedAt)
    .Limit(10)
          .ToListAsync();

     var velocities = completedSprints.Select(s => new SprintVelocity(
        s.Name,
                s.Capacity.CommittedStoryPoints,
  s.ActualVelocity ?? 0
   )).ToList();

     var avgVelocity = velocities.Any()
       ? velocities.Average(v => v.CompletedPoints)
             : 0;

return new VelocityData(projectId, velocities, (decimal)avgVelocity);
        }
        catch (MongoException ex)
        {
          _logger.LogError(ex, "Database error fetching velocity data for project {ProjectId}", projectId);
       throw new ServiceUnavailableException("Database", ex);
        }
    }

    private async Task RecordSprintMetrics(string sprintId)
  {
        try
        {
 var tasks = await _context.Tasks.Find(t => t.SprintId == sprintId).ToListAsync();
            var totalPoints = tasks.Where(t => t.StoryPoints.HasValue).Sum(t => t.StoryPoints!.Value);
      var completedPoints = tasks
           .Where(t => t.Status == Models.TaskStatus.Done && t.StoryPoints.HasValue)
    .Sum(t => t.StoryPoints!.Value);

    var tasksByStatus = tasks
          .GroupBy(t => t.Status.ToString())
       .ToDictionary(g => g.Key, g => g.Count());

    var metric = new SprintMetrics
   {
            SprintId = sprintId,
       Date = DateTime.UtcNow.Date,
        TotalStoryPoints = totalPoints,
            CompletedStoryPoints = completedPoints,
      RemainingStoryPoints = totalPoints - completedPoints,
    TasksByStatus = tasksByStatus
   };

            await _context.Metrics.InsertOneAsync(metric);
}
        catch (MongoException ex)
      {
   _logger.LogError(ex, "Error recording metrics for sprint {SprintId}", sprintId);
            // Don't throw - metrics are supplementary
        }
    }

    private async Task<SprintDto> MapToSprintDtoAsync(Sprint sprint)
    {
        var tasks = await _context.Tasks.Find(t => t.SprintId == sprint.Id).ToListAsync();

   var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t => t.Status == Models.TaskStatus.Done);
        var totalPoints = tasks.Where(t => t.StoryPoints.HasValue).Sum(t => t.StoryPoints!.Value);
      var completedPoints = tasks
  .Where(t => t.Status == Models.TaskStatus.Done && t.StoryPoints.HasValue)
        .Sum(t => t.StoryPoints!.Value);

        var completionPercentage = totalPoints > 0
            ? Math.Round((decimal)completedPoints / totalPoints * 100, 1)
  : 0;

        var stats = new SprintStats(
            totalTasks,
            completedTasks,
    totalPoints,
 completedPoints,
            completionPercentage
        );

        return new SprintDto(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
 sprint.SprintNumber,
        sprint.Status,
            sprint.StartDate,
            sprint.EndDate,
            sprint.Capacity,
            sprint.ActualVelocity,
 sprint.StartedAt,
    sprint.CompletedAt,
         sprint.Retrospective,
        stats
        );
 }
}
