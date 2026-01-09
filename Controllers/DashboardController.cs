using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SprintTracker.Api.Models.DTOs;
using SprintTracker.Api.Services;

namespace SprintTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
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

    /// <summary>
    /// Get dashboard statistics for the current user
    /// </summary>
    /// <returns>Dashboard statistics</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DashboardStats>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardStats>>> GetDashboardStats()
    {
  try
   {
  var stats = await _dashboardService.GetDashboardStatsAsync(GetUserId());
  return Ok(new ApiResponse<DashboardStats>(true, stats, null, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard stats for user {UserId}", GetUserId());
            throw;
        }
    }

    /// <summary>
    /// Get recent activity logs for the current user
    /// </summary>
    /// <param name="count">Number of activity items to return (default: 20, max: 100)</param>
    /// <returns>List of recent activity logs</returns>
    [HttpGet("activity")]
    [ProducesResponseType(typeof(ApiResponse<List<Models.ActivityLog>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<Models.ActivityLog>>>> GetRecentActivity([FromQuery] int count = 20)
    {
        // Validate count parameter
      count = Math.Clamp(count, 1, 100);

        try
        {
  var activity = await _dashboardService.GetRecentActivityAsync(GetUserId(), count);
          return Ok(new ApiResponse<List<Models.ActivityLog>>(true, activity, null, null));
   }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent activity for user {UserId}", GetUserId());
            throw;
        }
    }
}
