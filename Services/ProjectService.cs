using MongoDB.Driver;
using SprintTracker.Api.Data;
using SprintTracker.Api.Exceptions;
using SprintTracker.Api.Models;
using SprintTracker.Api.Models.DTOs;

namespace SprintTracker.Api.Services;

public interface IProjectService
{
    Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, string userId);
    Task<ProjectDto?> GetProjectByIdAsync(string projectId);
    Task<PaginatedResponse<ProjectDto>> GetProjectsAsync(string userId, int page = 1, int pageSize = 10);
    Task<ProjectDto?> UpdateProjectAsync(string projectId, UpdateProjectRequest request, string userId);
    Task<bool> DeleteProjectAsync(string projectId, string userId);
    Task<bool> AddTeamMemberAsync(string projectId, string memberId, string userId);
    Task<bool> RemoveTeamMemberAsync(string projectId, string memberId, string userId);
}

public class ProjectService : IProjectService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(MongoDbContext context, ILogger<ProjectService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Check if user has permission to manage a project (owner, manager on project team, or admin)
    /// </summary>
    private async Task<(bool HasPermission, User? CurrentUser, Project? Project)> CheckProjectPermissionAsync(
        string projectId, string userId, bool requireOwnerOrAdmin = false)
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

        // Owner always has access
    if (project.OwnerId == userId)
  return (true, currentUser, project);

        // If requiring owner or admin, managers don't get access
        if (requireOwnerOrAdmin)
   return (false, currentUser, project);

        // Managers who are team members can manage the project
   if (currentUser.Role == UserRole.Manager && project.TeamMemberIds.Contains(userId))
  return (true, currentUser, project);

        return (false, currentUser, project);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, string userId)
    {
 try
  {
  // Check for duplicate project key
    var existingProject = await _context.Projects
  .Find(p => p.Key == request.Key.ToUpper())
 .FirstOrDefaultAsync();

   if (existingProject != null)
            {
     throw new DuplicateResourceException("Project", request.Key);
    }

            // Verify user exists and can create projects (Admin or Manager)
            var currentUser = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
    if (currentUser == null)
      {
         throw new UnauthorizedAccessException("User not found");
    }

    if (currentUser.Role == UserRole.Developer)
     {
   throw new ForbiddenException("Developers cannot create projects. Only Admins and Managers can create projects.");
            }

            var project = new Project
            {
   Name = request.Name.Trim(),
                Key = request.Key.ToUpper().Trim(),
              Description = request.Description?.Trim(),
      OwnerId = userId,
 TeamMemberIds = new List<string> { userId },
        StartDate = request.StartDate,
        TargetEndDate = request.TargetEndDate,
 CreatedAt = DateTime.UtcNow,
     UpdatedAt = DateTime.UtcNow
            };

       await _context.Projects.InsertOneAsync(project);
     _logger.LogInformation("Project created: {ProjectName} (Key: {ProjectKey}) by user {UserId} (Role: {Role})", 
   project.Name, project.Key, userId, currentUser.Role);

  return await MapToProjectDtoAsync(project);
  }
    catch (DuplicateResourceException)
        {
  throw;
  }
        catch (ForbiddenException)
        {
            throw;
        }
    catch (UnauthorizedAccessException)
        {
     throw;
        }
   catch (MongoException ex)
      {
            _logger.LogError(ex, "Database error creating project");
         throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(string projectId)
    {
        try
        {
  var project = await _context.Projects.Find(p => p.Id == projectId).FirstOrDefaultAsync();
        return project != null ? await MapToProjectDtoAsync(project) : null;
        }
  catch (MongoException ex)
        {
            _logger.LogError(ex, "Database error fetching project {ProjectId}", projectId);
            throw new ServiceUnavailableException("Database", ex);
        }
    }

  public async Task<PaginatedResponse<ProjectDto>> GetProjectsAsync(string userId, int page = 1, int pageSize = 10)
    {
        try
    {
     // Get current user to check role
            var currentUser = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
      
    FilterDefinition<Project> filter;
       
            // Admins see all non-archived projects
if (currentUser?.Role == UserRole.Admin)
       {
         filter = Builders<Project>.Filter.Ne(p => p.Status, ProjectStatus.Archived);
          }
  else
         {
   // Other users see only projects they're part of
       filter = Builders<Project>.Filter.And(
         Builders<Project>.Filter.Ne(p => p.Status, ProjectStatus.Archived),
    Builders<Project>.Filter.Or(
           Builders<Project>.Filter.Eq(p => p.OwnerId, userId),
            Builders<Project>.Filter.AnyEq(p => p.TeamMemberIds, userId)
    )
                );
     }

            var totalCount = await _context.Projects.CountDocumentsAsync(filter);

var projects = await _context.Projects
     .Find(filter)
        .SortByDescending(p => p.UpdatedAt)
                .Skip((page - 1) * pageSize)
       .Limit(pageSize)
    .ToListAsync();

     var projectDtos = new List<ProjectDto>();
         foreach (var project in projects)
            {
     projectDtos.Add(await MapToProjectDtoAsync(project));
            }

       return new PaginatedResponse<ProjectDto>(
     projectDtos,
                (int)totalCount,
   page,
           pageSize,
           (int)Math.Ceiling((double)totalCount / pageSize)
     );
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Database error fetching projects for user {UserId}", userId);
    throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<ProjectDto?> UpdateProjectAsync(string projectId, UpdateProjectRequest request, string userId)
    {
 try
        {
      var (hasPermission, currentUser, project) = await CheckProjectPermissionAsync(projectId, userId);
    
    if (project == null) 
     return null;

            if (!hasPermission)
            {
    _logger.LogWarning("User {UserId} (Role: {Role}) attempted to update project {ProjectId} without permission", 
           userId, currentUser?.Role, projectId);
      throw new ForbiddenException("You don't have permission to update this project. Only the project owner, team managers, or administrators can update projects.");
            }

            var updateBuilder = Builders<Project>.Update.Set(p => p.UpdatedAt, DateTime.UtcNow);

 if (!string.IsNullOrWhiteSpace(request.Name))
updateBuilder = updateBuilder.Set(p => p.Name, request.Name.Trim());
            if (request.Description != null)
     updateBuilder = updateBuilder.Set(p => p.Description, request.Description.Trim());
            if (request.Status.HasValue)
      updateBuilder = updateBuilder.Set(p => p.Status, request.Status.Value);
   if (request.TargetEndDate.HasValue)
    updateBuilder = updateBuilder.Set(p => p.TargetEndDate, request.TargetEndDate.Value);
            if (request.TeamMemberIds != null)
            {
        // Ensure owner is always included in team members
       var memberIds = request.TeamMemberIds.ToList();
      if (!memberIds.Contains(project.OwnerId))
                {
        memberIds.Add(project.OwnerId);
           }
       
    // Validate all member IDs exist
            var validMemberIds = await ValidateMemberIdsAsync(memberIds);
       updateBuilder = updateBuilder.Set(p => p.TeamMemberIds, validMemberIds);
   }

          await _context.Projects.UpdateOneAsync(p => p.Id == projectId, updateBuilder);
    _logger.LogInformation("Project {ProjectId} updated by user {UserId} (Role: {Role})", 
 projectId, userId, currentUser?.Role);

            return await GetProjectByIdAsync(projectId);
        }
        catch (ForbiddenException)
      {
     throw;
   }
        catch (MongoException ex)
        {
      _logger.LogError(ex, "Database error updating project {ProjectId}", projectId);
        throw new ServiceUnavailableException("Database", ex);
  }
    }

  public async Task<bool> DeleteProjectAsync(string projectId, string userId)
    {
        try
        {
            // Only owner or admin can delete/archive projects
        var (hasPermission, currentUser, project) = await CheckProjectPermissionAsync(projectId, userId, requireOwnerOrAdmin: true);
   
   if (project == null) 
      return false;

            if (!hasPermission)
   {
      _logger.LogWarning("User {UserId} (Role: {Role}) attempted to delete project {ProjectId} without permission", 
 userId, currentUser?.Role, projectId);
                return false;
            }

            // Soft delete by setting status to Archived
       var update = Builders<Project>.Update
       .Set(p => p.Status, ProjectStatus.Archived)
         .Set(p => p.UpdatedAt, DateTime.UtcNow);

            await _context.Projects.UpdateOneAsync(p => p.Id == projectId, update);
            _logger.LogInformation("Project {ProjectId} archived by user {UserId} (Role: {Role})", 
     projectId, userId, currentUser?.Role);
 return true;
      }
        catch (MongoException ex)
        {
         _logger.LogError(ex, "Database error deleting project {ProjectId}", projectId);
      throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<bool> AddTeamMemberAsync(string projectId, string memberId, string userId)
    {
        try
  {
            var (hasPermission, currentUser, project) = await CheckProjectPermissionAsync(projectId, userId);

         if (project == null || !hasPermission) 
       return false;

            // Check if member exists
       var memberExists = await _context.Users.Find(u => u.Id == memberId && u.IsActive).AnyAsync();
if (!memberExists)
            {
    _logger.LogWarning("Attempted to add non-existent or inactive user {MemberId} to project {ProjectId}", 
  memberId, projectId);
  return false;
    }

    // Check if already a member
         if (project.TeamMemberIds.Contains(memberId))
    {
          return true; // Already a member, consider it success
      }

            var update = Builders<Project>.Update
                .AddToSet(p => p.TeamMemberIds, memberId)
         .Set(p => p.UpdatedAt, DateTime.UtcNow);

            await _context.Projects.UpdateOneAsync(p => p.Id == projectId, update);
    _logger.LogInformation("User {MemberId} added to project {ProjectId} by {UserId} (Role: {Role})", 
    memberId, projectId, userId, currentUser?.Role);
            return true;
        }
        catch (MongoException ex)
        {
    _logger.LogError(ex, "Database error adding team member to project {ProjectId}", projectId);
   throw new ServiceUnavailableException("Database", ex);
  }
    }

    public async Task<bool> RemoveTeamMemberAsync(string projectId, string memberId, string userId)
    {
        try
     {
            var (hasPermission, currentUser, project) = await CheckProjectPermissionAsync(projectId, userId);
   
          if (project == null || !hasPermission) 
         return false;

            // Cannot remove the owner
  if (memberId == project.OwnerId)
       {
             _logger.LogWarning("Attempted to remove owner {OwnerId} from project {ProjectId}", 
     memberId, projectId);
    return false;
      }

   var update = Builders<Project>.Update
       .Pull(p => p.TeamMemberIds, memberId)
  .Set(p => p.UpdatedAt, DateTime.UtcNow);

     await _context.Projects.UpdateOneAsync(p => p.Id == projectId, update);
            _logger.LogInformation("User {MemberId} removed from project {ProjectId} by {UserId} (Role: {Role})", 
     memberId, projectId, userId, currentUser?.Role);
            return true;
        }
        catch (MongoException ex)
        {
    _logger.LogError(ex, "Database error removing team member from project {ProjectId}", projectId);
     throw new ServiceUnavailableException("Database", ex);
        }
    }

    /// <summary>
    /// Validate member IDs exist and are active users
    /// </summary>
    private async Task<List<string>> ValidateMemberIdsAsync(List<string> memberIds)
    {
        if (memberIds == null || memberIds.Count == 0)
          return new List<string>();

     var validUsers = await _context.Users
            .Find(Builders<User>.Filter.And(
                Builders<User>.Filter.In(u => u.Id, memberIds),
      Builders<User>.Filter.Eq(u => u.IsActive, true)
        ))
  .Project(u => u.Id)
            .ToListAsync();

 return validUsers;
    }

    private async Task<ProjectDto> MapToProjectDtoAsync(Project project)
    {
var teamMembers = await _context.Users
      .Find(Builders<User>.Filter.In(u => u.Id, project.TeamMemberIds))
 .ToListAsync();

   return new ProjectDto(
            project.Id,
            project.Name,
  project.Key,
     project.Description,
        project.OwnerId,
            teamMembers.Select(u => new UserDto(
    u.Id, u.Email, u.FirstName, u.LastName, u.FullName, u.Role, u.Avatar, u.IsActive
         )).ToList(),
       project.Status,
  project.StartDate,
         project.TargetEndDate,
          project.Settings,
     project.CreatedAt
        );
    }
}
