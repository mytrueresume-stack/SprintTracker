using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SprintTracker.Api.Exceptions;
using SprintTracker.Api.Models.DTOs;
using SprintTracker.Api.Services;

namespace SprintTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    private string GetUserId()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
      {
            throw new UnauthorizedAccessException("User is not authenticated");
        }
        return userId;
    }

    private string GetUserRole()
{
        return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Developer";
    }

  /// <summary>
    /// Create a new project (Admin and Manager only)
    /// </summary>
    /// <param name="request">Project creation details</param>
    /// <returns>Created project</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProject([FromBody] CreateProjectRequestDto request)
    {
   // Null body
        if (request == null)
 {
      _logger.LogWarning("CreateProject called with null body");
     return BadRequest(new ApiResponse<ProjectDto>(false, null, "Request body is required", null));
}

        // Validate model state (normalization filter already ran)
    if (!ModelState.IsValid)
      {
            var errors = ModelState.Values
     .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
         .ToList();

  _logger.LogWarning("CreateProject validation failed for user {User}: {Errors}",
User?.Identity?.Name ?? "Anonymous", string.Join(", ", errors));

      return BadRequest(new ApiResponse<ProjectDto>(false, null, "Validation failed", errors));
  }

        // Ensure user is authenticated
     if (string.IsNullOrEmpty(User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value))
        {
  _logger.LogWarning("CreateProject attempted by anonymous user");
       return Unauthorized(new ApiResponse<ProjectDto>(false, null, "Authentication required", null));
        }

        // Authorization: only Manager or Admin can create projects
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Developer";
        if (roleClaim != SprintTracker.Api.Models.UserRole.Manager.ToString() && roleClaim != SprintTracker.Api.Models.UserRole.Admin.ToString())
        {
            _logger.LogWarning("CreateProject forbidden for user {User} with role {Role}", User?.Identity?.Name ?? "Unknown", roleClaim);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<ProjectDto>(false, null, "Insufficient permissions", null));
        }

        // Map DTO to domain request with defensive normalization
        var createReq = new CreateProjectRequest(
   request.Name?.Trim() ?? string.Empty,
    (request.Key ?? string.Empty).ToUpperInvariant().Trim(),
            request.Description?.Trim(),
            request.StartDate,
            request.TargetEndDate
        );

        try
        {
     var projectDto = await _projectService.CreateProjectAsync(createReq, GetUserId());
     _logger.LogInformation("Project created: {ProjectId} by user {UserId} (Role: {Role})", 
     projectDto.Id, GetUserId(), GetUserRole());
 return CreatedAtAction(nameof(GetProject), new { id = projectDto.Id }, 
           new ApiResponse<ProjectDto>(true, projectDto, "Project created successfully", null));
        }
        catch (DuplicateResourceException ex)
        {
        _logger.LogWarning(ex, "Duplicate project key attempted by user {UserId}", GetUserId());
        return Conflict(new ApiResponse<ProjectDto>(false, null, ex.Message, new List<string> { ex.Message }));
        }
    catch (ForbiddenException ex)
        {
_logger.LogWarning(ex, "User {UserId} (Role: {Role}) not authorized to create projects", 
     GetUserId(), GetUserRole());
            return StatusCode(StatusCodes.Status403Forbidden, 
     new ApiResponse<ProjectDto>(false, null, ex.Message, null));
        }
        catch (UnauthorizedAccessException ex)
    {
            _logger.LogWarning(ex, "Unauthorized user attempted to create project");
  return Unauthorized(new ApiResponse<ProjectDto>(false, null, ex.Message, null));
        }
        catch (ServiceUnavailableException ex)
        {
     _logger.LogError(ex, "Database/service error creating project");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
 new ApiResponse<ProjectDto>(false, null, "Service unavailable", new List<string> { ex.Message }));
     }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Unexpected error creating project");
       return StatusCode(StatusCodes.Status500InternalServerError, 
         new ApiResponse<ProjectDto>(false, null, "An unexpected error occurred", null));
        }
 }

    /// <summary>
/// Get a project by ID
    /// </summary>
    /// <param name="id">Project ID</param>
  /// <returns>Project details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProject(string id)
 {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiResponse<ProjectDto>(false, null, "Project ID is required", null));
        }

    var project = await _projectService.GetProjectByIdAsync(id);
   if (project == null)
   {
            return NotFound(new ApiResponse<ProjectDto>(false, null, "Project not found", null));
        }

 return Ok(new ApiResponse<ProjectDto>(true, project, null, null));
    }

    /// <summary>
/// Get all projects for the current user
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 50)</param>
    /// <returns>Paginated list of projects</returns>
  [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ProjectDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ProjectDto>>>> GetProjects(
   [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
    // Validate pagination parameters
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var projects = await _projectService.GetProjectsAsync(GetUserId(), page, pageSize);
        return Ok(new ApiResponse<PaginatedResponse<ProjectDto>>(true, projects, null, null));
    }

  /// <summary>
    /// Update an existing project
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="request">Project update details</param>
    /// <returns>Updated project</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateProject(string id, [FromBody] UpdateProjectRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
         return BadRequest(new ApiResponse<ProjectDto>(false, null, "Project ID is required", null));
 }

        if (!ModelState.IsValid)
        {
    var errors = ModelState.Values
        .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
      .ToList();
     return BadRequest(new ApiResponse<ProjectDto>(false, null, "Validation failed", errors));
        }

        try
        {
 var updateRequest = new UpdateProjectRequest(
          request.Name,
         request.Description,
   request.Status,
        request.TargetEndDate,
    request.TeamMemberIds
            );

       var project = await _projectService.UpdateProjectAsync(id, updateRequest, GetUserId());
          if (project == null)
            {
       return NotFound(new ApiResponse<ProjectDto>(false, null, "Project not found", null));
            }

            _logger.LogInformation("Project updated: {ProjectId} by user {UserId} (Role: {Role})", 
     id, GetUserId(), GetUserRole());
         return Ok(new ApiResponse<ProjectDto>(true, project, "Project updated successfully", null));
    }
  catch (ForbiddenException ex)
        {
  return StatusCode(StatusCodes.Status403Forbidden, 
         new ApiResponse<ProjectDto>(false, null, ex.Message, null));
        }
    }

    /// <summary>
    /// Delete (archive) a project
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
 public async Task<ActionResult<ApiResponse<bool>>> DeleteProject(string id)
    {
    if (string.IsNullOrWhiteSpace(id))
 {
            return BadRequest(new ApiResponse<bool>(false, false, "Project ID is required", null));
        }

        var result = await _projectService.DeleteProjectAsync(id, GetUserId());
        if (!result)
        {
            return NotFound(new ApiResponse<bool>(false, false, "Project not found or you don't have permission to delete it", null));
     }

        _logger.LogInformation("Project archived: {ProjectId} by user {UserId} (Role: {Role})", 
     id, GetUserId(), GetUserRole());
        return Ok(new ApiResponse<bool>(true, true, "Project archived successfully", null));
    }

  /// <summary>
    /// Add a team member to a project
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="memberId">User ID to add</param>
    /// <returns>Success status</returns>
  [HttpPost("{id}/members/{memberId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
 [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> AddTeamMember(string id, string memberId)
    {
     if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(memberId))
        {
         return BadRequest(new ApiResponse<bool>(false, false, "Project ID and Member ID are required", null));
        }

      var result = await _projectService.AddTeamMemberAsync(id, memberId, GetUserId());
        if (!result)
        {
         return BadRequest(new ApiResponse<bool>(false, false, "Failed to add team member. Project not found or you don't have permission.", null));
        }

     _logger.LogInformation("Team member {MemberId} added to project {ProjectId} by user {UserId} (Role: {Role})", 
    memberId, id, GetUserId(), GetUserRole());
        return Ok(new ApiResponse<bool>(true, true, "Team member added successfully", null));
    }

    /// <summary>
    /// Update team members for a project (bulk update)
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="request">List of user IDs to set as team members</param>
    /// <returns>Updated project</returns>
    [HttpPost("{id}/members")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateTeamMembers(string id, [FromBody] UpdateTeamMembersRequest request)
    {
        _logger.LogInformation("UpdateTeamMembers called for project {ProjectId} by user {UserId}. Request: {Request}", 
  id, GetUserId(), request != null ? JsonSerializer.Serialize(request) : "null");

        if (string.IsNullOrWhiteSpace(id))
     {
        return BadRequest(new ApiResponse<ProjectDto>(false, null, "Project ID is required", null));
  }

        if (request == null)
        {
  _logger.LogWarning("UpdateTeamMembers called with null request body for project {ProjectId}", id);
  return BadRequest(new ApiResponse<ProjectDto>(false, null, "Request body is required", null));
  }

        if (request.MemberIds == null)
        {
   _logger.LogWarning("UpdateTeamMembers called with null MemberIds for project {ProjectId}", id);
          return BadRequest(new ApiResponse<ProjectDto>(false, null, "Member IDs are required", null));
        }

  try
        {
            // Update project with new team members
 var updateRequest = new UpdateProjectRequest(null, null, null, null, request.MemberIds);
     var project = await _projectService.UpdateProjectAsync(id, updateRequest, GetUserId());
        
            if (project == null)
            {
return NotFound(new ApiResponse<ProjectDto>(false, null, "Project not found or you don't have permission", null));
   }

    _logger.LogInformation("Team members updated for project {ProjectId} by user {UserId} (Role: {Role}). Count: {Count}", 
      id, GetUserId(), GetUserRole(), request.MemberIds.Count);
   return Ok(new ApiResponse<ProjectDto>(true, project, "Team members updated successfully", null));
        }
        catch (ForbiddenException ex)
     {
     _logger.LogWarning(ex, "User {UserId} not authorized to update team members for project {ProjectId}", 
      GetUserId(), id);
            return StatusCode(StatusCodes.Status403Forbidden, 
              new ApiResponse<ProjectDto>(false, null, ex.Message, null));
        }
    }

    /// <summary>
    /// Remove a team member from a project
    /// </summary>
  /// <param name="id">Project ID</param>
    /// <param name="memberId">User ID to remove</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}/members/{memberId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveTeamMember(string id, string memberId)
    {
    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(memberId))
 {
        return BadRequest(new ApiResponse<bool>(false, false, "Project ID and Member ID are required", null));
     }

  var result = await _projectService.RemoveTeamMemberAsync(id, memberId, GetUserId());
 if (!result)
        {
          return BadRequest(new ApiResponse<bool>(false, false, "Failed to remove team member. Project not found or you don't have permission.", null));
  }

      _logger.LogInformation("Team member {MemberId} removed from project {ProjectId} by user {UserId} (Role: {Role})", 
            memberId, id, GetUserId(), GetUserRole());
        return Ok(new ApiResponse<bool>(true, true, "Team member removed successfully", null));
    }
}
