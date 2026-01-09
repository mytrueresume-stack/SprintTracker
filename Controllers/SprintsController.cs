using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SprintTracker.Api.Exceptions;
using SprintTracker.Api.Models;
using SprintTracker.Api.Models.DTOs;
using SprintTracker.Api.Services;

namespace SprintTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class SprintsController : ControllerBase
{
    private readonly ISprintService _sprintService;
    private readonly ILogger<SprintsController> _logger;

    public SprintsController(ISprintService sprintService, ILogger<SprintsController> logger)
    {
     _sprintService = sprintService;
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
    /// Create a new sprint (Admin, Manager, or Project Owner only)
    /// </summary>
    /// <param name="request">Sprint creation details</param>
    /// <returns>Created sprint</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SprintDto>>> CreateSprint([FromBody] CreateSprintRequestDto request)
    {
        if (!ModelState.IsValid)
    {
        var errors = ModelState.Values
    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
        .ToList();
      return BadRequest(new ApiResponse<SprintDto>(false, null, "Validation failed", errors));
  }

        try
        {
          var createRequest = new CreateSprintRequest(
       request.ProjectId,
       request.Name,
    request.Goal,
                request.StartDate,
          request.EndDate
    );

      var sprint = await _sprintService.CreateSprintAsync(createRequest, GetUserId());
        _logger.LogInformation("Sprint created: {SprintId} for project {ProjectId} by user {UserId} (Role: {Role})", 
  sprint.Id, request.ProjectId, GetUserId(), GetUserRole());
        
            return CreatedAtAction(nameof(GetSprint), new { id = sprint.Id },
         new ApiResponse<SprintDto>(true, sprint, "Sprint created successfully", null));
        }
        catch (NotFoundException ex)
  {
            return NotFound(new ApiResponse<SprintDto>(false, null, ex.Message, null));
    }
        catch (ForbiddenException ex)
     {
            return StatusCode(StatusCodes.Status403Forbidden, 
       new ApiResponse<SprintDto>(false, null, ex.Message, null));
        }
    catch (BusinessRuleViolationException ex)
    {
        return BadRequest(new ApiResponse<SprintDto>(false, null, ex.Message, null));
        }
    }

    /// <summary>
    /// Get a sprint by ID
    /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <returns>Sprint details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SprintDto>>> GetSprint(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiResponse<SprintDto>(false, null, "Sprint ID is required", null));
    }

        var sprint = await _sprintService.GetSprintByIdAsync(id);
if (sprint == null)
   {
  return NotFound(new ApiResponse<SprintDto>(false, null, "Sprint not found", null));
        }

        return Ok(new ApiResponse<SprintDto>(true, sprint, null, null));
    }

    /// <summary>
    /// Get all sprints for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 50)</param>
    /// <returns>Paginated list of sprints</returns>
    [HttpGet("project/{projectId}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<SprintDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SprintDto>>>> GetSprintsByProject(
        string projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
   return BadRequest(new ApiResponse<PaginatedResponse<SprintDto>>(false, null, "Project ID is required", null));
  }

  page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var sprints = await _sprintService.GetSprintsByProjectAsync(projectId, page, pageSize);
        return Ok(new ApiResponse<PaginatedResponse<SprintDto>>(true, sprints, null, null));
    }

    /// <summary>
 /// Get the currently active sprint for a project
    /// </summary>
 /// <param name="projectId">Project ID</param>
    /// <returns>Active sprint details</returns>
    [HttpGet("project/{projectId}/active")]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SprintDto>>> GetActiveSprint(string projectId)
    {
     if (string.IsNullOrWhiteSpace(projectId))
        {
   return BadRequest(new ApiResponse<SprintDto>(false, null, "Project ID is required", null));
        }

        var sprint = await _sprintService.GetActiveSprintAsync(projectId);
        if (sprint == null)
     {
            return NotFound(new ApiResponse<SprintDto>(false, null, "No active sprint found for this project", null));
        }

        return Ok(new ApiResponse<SprintDto>(true, sprint, null, null));
    }

    /// <summary>
    /// Update an existing sprint
    /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <param name="request">Sprint update details</param>
    /// <returns>Updated sprint</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status400BadRequest)]
 [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SprintDto>>> UpdateSprint(string id, [FromBody] UpdateSprintRequestDto request)
    {
   if (string.IsNullOrWhiteSpace(id))
        {
return BadRequest(new ApiResponse<SprintDto>(false, null, "Sprint ID is required", null));
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
      .ToList();
        return BadRequest(new ApiResponse<SprintDto>(false, null, "Validation failed", errors));
        }

 try
    {
    var updateRequest = new UpdateSprintRequest(
            request.Name,
    request.Goal,
    request.Status,
request.StartDate,
       request.EndDate,
    request.Capacity
            );

            var sprint = await _sprintService.UpdateSprintAsync(id, updateRequest, GetUserId());
            if (sprint == null)
        {
      return NotFound(new ApiResponse<SprintDto>(false, null, "Sprint not found", null));
  }

            _logger.LogInformation("Sprint updated: {SprintId} by user {UserId} (Role: {Role})", 
                id, GetUserId(), GetUserRole());
        return Ok(new ApiResponse<SprintDto>(true, sprint, "Sprint updated successfully", null));
   }
        catch (ForbiddenException ex)
        {
      return StatusCode(StatusCodes.Status403Forbidden, 
      new ApiResponse<SprintDto>(false, null, ex.Message, null));
        }
      catch (BusinessRuleViolationException ex)
        {
        return BadRequest(new ApiResponse<SprintDto>(false, null, ex.Message, null));
        }
    }

    /// <summary>
    /// Start a sprint
    /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <returns>Started sprint</returns>
    [HttpPost("{id}/start")]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SprintDto>>> StartSprint(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
      return BadRequest(new ApiResponse<SprintDto>(false, null, "Sprint ID is required", null));
 }

        try
        {
    var sprint = await _sprintService.StartSprintAsync(id, GetUserId());
      if (sprint == null)
            {
            return BadRequest(new ApiResponse<SprintDto>(false, null, 
   "Cannot start sprint. Sprint not found or is not in planning status.", null));
          }

  _logger.LogInformation("Sprint started: {SprintId} by user {UserId} (Role: {Role})", 
        id, GetUserId(), GetUserRole());
            return Ok(new ApiResponse<SprintDto>(true, sprint, "Sprint started successfully", null));
  }
        catch (ForbiddenException ex)
   {
     return StatusCode(StatusCodes.Status403Forbidden, 
            new ApiResponse<SprintDto>(false, null, ex.Message, null));
        }
        catch (BusinessRuleViolationException ex)
        {
            return BadRequest(new ApiResponse<SprintDto>(false, null, ex.Message, null));
        }
    }

    /// <summary>
    /// Complete a sprint
 /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <param name="retrospective">Optional retrospective notes</param>
    /// <returns>Completed sprint</returns>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SprintDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SprintDto>>> CompleteSprint(string id, [FromBody] SprintRetrospective? retrospective = null)
    {
  if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiResponse<SprintDto>(false, null, "Sprint ID is required", null));
        }

 try
   {
        var sprint = await _sprintService.CompleteSprintAsync(id, retrospective, GetUserId());
            if (sprint == null)
{
       return BadRequest(new ApiResponse<SprintDto>(false, null, 
         "Cannot complete sprint. Sprint not found or is not active.", null));
        }

          _logger.LogInformation("Sprint completed: {SprintId} with velocity {Velocity} by user {UserId} (Role: {Role})", 
      id, sprint.ActualVelocity, GetUserId(), GetUserRole());
          return Ok(new ApiResponse<SprintDto>(true, sprint, "Sprint completed successfully", null));
     }
      catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
      new ApiResponse<SprintDto>(false, null, ex.Message, null));
      }
  catch (BusinessRuleViolationException ex)
  {
         return BadRequest(new ApiResponse<SprintDto>(false, null, ex.Message, null));
      }
    }

    /// <summary>
    /// Delete a sprint
    /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <returns>Success status</returns>
 [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSprint(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
      {
            return BadRequest(new ApiResponse<bool>(false, false, "Sprint ID is required", null));
        }

        try
 {
            var result = await _sprintService.DeleteSprintAsync(id, GetUserId());
   if (!result)
 {
      return BadRequest(new ApiResponse<bool>(false, false, 
          "Cannot delete sprint. Sprint not found or is currently active.", null));
   }

  _logger.LogInformation("Sprint deleted: {SprintId} by user {UserId} (Role: {Role})", 
    id, GetUserId(), GetUserRole());
            return Ok(new ApiResponse<bool>(true, true, "Sprint deleted successfully", null));
        }
        catch (ForbiddenException ex)
        {
   return StatusCode(StatusCodes.Status403Forbidden, 
    new ApiResponse<bool>(false, false, ex.Message, null));
        }
 catch (BusinessRuleViolationException ex)
        {
  return BadRequest(new ApiResponse<bool>(false, false, ex.Message, null));
        }
    }

    /// <summary>
    /// Get burndown data for a sprint
  /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <returns>Burndown chart data</returns>
    [HttpGet("{id}/burndown")]
    [ProducesResponseType(typeof(ApiResponse<BurndownData>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BurndownData>>> GetBurndownData(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
return BadRequest(new ApiResponse<BurndownData>(false, null, "Sprint ID is required", null));
        }

        var data = await _sprintService.GetBurndownDataAsync(id);
     return Ok(new ApiResponse<BurndownData>(true, data, null, null));
 }

    /// <summary>
    /// Get velocity data for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Velocity chart data</returns>
    [HttpGet("project/{projectId}/velocity")]
    [ProducesResponseType(typeof(ApiResponse<VelocityData>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VelocityData>>> GetVelocityData(string projectId)
    {
   if (string.IsNullOrWhiteSpace(projectId))
   {
    return BadRequest(new ApiResponse<VelocityData>(false, null, "Project ID is required", null));
        }

        var data = await _sprintService.GetVelocityDataAsync(projectId);
      return Ok(new ApiResponse<VelocityData>(true, data, null, null));
    }
}
