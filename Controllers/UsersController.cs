using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MongoDB.Driver;
using SprintTracker.Api.Data;
using SprintTracker.Api.Exceptions;
using SprintTracker.Api.Models;
using SprintTracker.Api.Models.DTOs;

namespace SprintTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
 private readonly MongoDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(MongoDbContext context, ILogger<UsersController> logger)
    {
  _context = context;
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

    private async Task<User?> GetCurrentUserAsync()
    {
 var userId = GetUserId();
 return await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get all users with optional search and role filters
    /// Only admins and managers can view all users
    /// Developers can only view users in their projects
    /// </summary>
    /// <param name="search">Search term for name or email</param>
    /// <param name="role">Filter by user role</param>
    /// <returns>List of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsers([FromQuery] string? search, [FromQuery] UserRole? role)
    {
   try
{
     var currentUser = await GetCurrentUserAsync();
      if (currentUser == null)
  {
     return Unauthorized(new ApiResponse<List<UserDto>>(false, null, "User not found", null));
    }

      var filterBuilder = Builders<User>.Filter;
    var filters = new List<FilterDefinition<User>> { filterBuilder.Eq(u => u.IsActive, true) };

       if (!string.IsNullOrEmpty(search))
  {
    var searchTerm = search.Trim();
      // Escape special regex characters for safety
          var escapedSearch = System.Text.RegularExpressions.Regex.Escape(searchTerm);
    
     filters.Add(filterBuilder.Or(
     filterBuilder.Regex(u => u.FirstName, new MongoDB.Bson.BsonRegularExpression(escapedSearch, "i")),
           filterBuilder.Regex(u => u.LastName, new MongoDB.Bson.BsonRegularExpression(escapedSearch, "i")),
   filterBuilder.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(escapedSearch, "i"))
    ));
   }

            if (role.HasValue)
       {
   filters.Add(filterBuilder.Eq(u => u.Role, role.Value));
   }

   var users = await _context.Users
          .Find(filterBuilder.And(filters))
       .SortBy(u => u.FirstName)
   .ThenBy(u => u.LastName)
         .Limit(100) // Prevent unbounded queries
      .ToListAsync();

  var userDtos = users.Select(u => new UserDto(
   u.Id, u.Email, u.FirstName, u.LastName, u.FullName, u.Role, u.Avatar, u.IsActive
            )).ToList();

      _logger.LogDebug("User {UserId} (Role: {Role}) retrieved {Count} users", 
           currentUser.Id, currentUser.Role, userDtos.Count);

   return Ok(new ApiResponse<List<UserDto>>(true, userDtos, null, null));
        }
        catch (Exception ex)
  {
   _logger.LogError(ex, "Error fetching users");
          throw;
        }
    }

  /// <summary>
    /// Get a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
 [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
return BadRequest(new ApiResponse<UserDto>(false, null, "User ID is required", null));
    }

        try
        {
 var user = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
   if (user == null)
        {
 return NotFound(new ApiResponse<UserDto>(false, null, "User not found", null));
    }

            return Ok(new ApiResponse<UserDto>(true,
   new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.Role, user.Avatar, user.IsActive),
    null, null));
        }
      catch (Exception ex)
   {
       _logger.LogError(ex, "Error fetching user {UserId}", id);
            throw;
  }
    }

    /// <summary>
    /// Update a user's profile
    /// Users can update their own profile; Admins can update any user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Update details</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(string id, [FromBody] UpdateUserRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
       return BadRequest(new ApiResponse<UserDto>(false, null, "User ID is required", null));
        }

 if (!ModelState.IsValid)
 {
    var errors = ModelState.Values
         .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
 .ToList();
         return BadRequest(new ApiResponse<UserDto>(false, null, "Validation failed", errors));
   }

        try
        {
            var currentUserId = GetUserId();
 var currentUser = await _context.Users.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();

  // Only allow self-update or admin update
     if (currentUserId != id && currentUser?.Role != UserRole.Admin)
         {
  _logger.LogWarning("User {CurrentUserId} (Role: {Role}) attempted to update user {TargetUserId} without permission", 
    currentUserId, currentUser?.Role, id);
       return StatusCode(StatusCodes.Status403Forbidden, 
        new ApiResponse<UserDto>(false, null, "You don't have permission to update this user", null));
     }

       var updateBuilder = Builders<User>.Update.Set(u => u.UpdatedAt, DateTime.UtcNow);

    if (!string.IsNullOrEmpty(request.FirstName))
updateBuilder = updateBuilder.Set(u => u.FirstName, request.FirstName.Trim());
       if (!string.IsNullOrEmpty(request.LastName))
  updateBuilder = updateBuilder.Set(u => u.LastName, request.LastName.Trim());
  if (!string.IsNullOrEmpty(request.Avatar))
  updateBuilder = updateBuilder.Set(u => u.Avatar, request.Avatar);

     // Only admin can change roles
   if (request.Role.HasValue && currentUser?.Role == UserRole.Admin)
      {
  updateBuilder = updateBuilder.Set(u => u.Role, request.Role.Value);
 _logger.LogInformation("Admin {AdminId} changed role of user {UserId} to {Role}", 
      currentUserId, id, request.Role.Value);
      }

         var result = await _context.Users.UpdateOneAsync(u => u.Id == id, updateBuilder);
     if (result.MatchedCount == 0)
     {
   return NotFound(new ApiResponse<UserDto>(false, null, "User not found", null));
     }

     _logger.LogInformation("User {UserId} updated by {CurrentUserId}", id, currentUserId);
     return await GetUser(id);
        }
        catch (Exception ex)
  {
       _logger.LogError(ex, "Error updating user {UserId}", id);
     throw;
        }
 }

    /// <summary>
    /// Get team members for a project
/// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>List of team members</returns>
    [HttpGet("team/{projectId}")]
    [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetProjectTeam(string projectId)
    {
 if (string.IsNullOrWhiteSpace(projectId))
        {
            return BadRequest(new ApiResponse<List<UserDto>>(false, null, "Project ID is required", null));
 }

        try
        {
    var project = await _context.Projects.Find(p => p.Id == projectId).FirstOrDefaultAsync();
            if (project == null)
     {
     return NotFound(new ApiResponse<List<UserDto>>(false, null, "Project not found", null));
         }

            List<User> users;

         // If project has team members, return them; otherwise return all active users
     if (project.TeamMemberIds != null && project.TeamMemberIds.Any())
            {
    users = await _context.Users
   .Find(Builders<User>.Filter.In(u => u.Id, project.TeamMemberIds))
     .SortBy(u => u.FirstName)
    .ThenBy(u => u.LastName)
    .ToListAsync();
  }
   else
     {
    // Return all active users as potential team members
  users = await _context.Users
       .Find(u => u.IsActive)
   .SortBy(u => u.FirstName)
          .ThenBy(u => u.LastName)
         .Limit(100) // Prevent unbounded queries
  .ToListAsync();
            }

var userDtos = users.Select(u => new UserDto(
   u.Id, u.Email, u.FirstName, u.LastName, u.FullName, u.Role, u.Avatar, u.IsActive
        )).ToList();

return Ok(new ApiResponse<List<UserDto>>(true, userDtos, null, null));
        }
  catch (Exception ex)
   {
 _logger.LogError(ex, "Error fetching team for project {ProjectId}", projectId);
         throw;
 }
    }

    /// <summary>
    /// Deactivate a user (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateUser(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
{
     return BadRequest(new ApiResponse<bool>(false, false, "User ID is required", null));
        }

        try
   {
  var currentUser = await GetCurrentUserAsync();
          if (currentUser?.Role != UserRole.Admin)
  {
       _logger.LogWarning("Non-admin user {UserId} (Role: {Role}) attempted to deactivate user {TargetUserId}", 
     GetUserId(), currentUser?.Role, id);
          return StatusCode(StatusCodes.Status403Forbidden, 
     new ApiResponse<bool>(false, false, "Only administrators can deactivate users", null));
            }

       // Prevent self-deactivation
     if (id == currentUser.Id)
            {
      return BadRequest(new ApiResponse<bool>(false, false, "You cannot deactivate your own account", null));
  }

var update = Builders<User>.Update
     .Set(u => u.IsActive, false)
    .Set(u => u.UpdatedAt, DateTime.UtcNow);

var result = await _context.Users.UpdateOneAsync(u => u.Id == id, update);
  if (result.MatchedCount == 0)
            {
       return NotFound(new ApiResponse<bool>(false, false, "User not found", null));
            }

        _logger.LogInformation("User {UserId} deactivated by admin {AdminId}", id, currentUser.Id);
            return Ok(new ApiResponse<bool>(true, true, "User deactivated successfully", null));
  }
catch (Exception ex)
        {
_logger.LogError(ex, "Error deactivating user {UserId}", id);
       throw;
        }
    }

    /// <summary>
    /// Reactivate a user (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/reactivate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
 [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> ReactivateUser(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
   {
 return BadRequest(new ApiResponse<bool>(false, false, "User ID is required", null));
        }

        try
{
        var currentUser = await GetCurrentUserAsync();
        if (currentUser?.Role != UserRole.Admin)
          {
        _logger.LogWarning("Non-admin user {UserId} (Role: {Role}) attempted to reactivate user {TargetUserId}", 
      GetUserId(), currentUser?.Role, id);
          return StatusCode(StatusCodes.Status403Forbidden, 
        new ApiResponse<bool>(false, false, "Only administrators can reactivate users", null));
            }

            var update = Builders<User>.Update
   .Set(u => u.IsActive, true)
       .Set(u => u.UpdatedAt, DateTime.UtcNow);

     var result = await _context.Users.UpdateOneAsync(u => u.Id == id, update);
            if (result.MatchedCount == 0)
   {
           return NotFound(new ApiResponse<bool>(false, false, "User not found", null));
   }

   _logger.LogInformation("User {UserId} reactivated by admin {AdminId}", id, currentUser.Id);
      return Ok(new ApiResponse<bool>(true, true, "User reactivated successfully", null));
        }
   catch (Exception ex)
    {
    _logger.LogError(ex, "Error reactivating user {UserId}", id);
        throw;
        }
    }
}
