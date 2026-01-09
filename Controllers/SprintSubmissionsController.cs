using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SprintTracker.Api.Models;
using SprintTracker.Api.Models.DTOs;
using SprintTracker.Api.Services;
using SprintTracker.Api.Exceptions;

namespace SprintTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class SprintSubmissionsController : ControllerBase
{
    private readonly ISprintSubmissionService _submissionService;
    private readonly ISprintService _sprintService;
    private readonly ILogger<SprintSubmissionsController> _logger;

    public SprintSubmissionsController(
        ISprintSubmissionService submissionService,
        ISprintService sprintService,
   ILogger<SprintSubmissionsController> logger)
    {
   _submissionService = submissionService;
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

    /// <summary>
    /// Get or create a submission for the current user for a specific sprint
/// </summary>
    /// <param name="sprintId">Sprint ID</param>
    /// <returns>Sprint submission</returns>
    [HttpGet("sprint/{sprintId}")]
    [ProducesResponseType(typeof(ApiResponse<SprintSubmission>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintSubmission>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SprintSubmission>>> GetMySubmission(string sprintId)
    {
        if (string.IsNullOrWhiteSpace(sprintId))
  {
  return BadRequest(new ApiResponse<SprintSubmission>(false, null, "Sprint ID is required", null));
  }

 try
        {
        var userId = GetUserId();
     var submission = await _submissionService.GetSubmissionAsync(sprintId, userId);

      if (submission == null)
   {
    // Return empty submission template
     var sprint = await _sprintService.GetSprintByIdAsync(sprintId);
  if (sprint == null)
 {
             return NotFound(new ApiResponse<SprintSubmission>(false, null, "Sprint not found", null));
     }

         submission = new SprintSubmission
 {
 SprintId = sprintId,
    ProjectId = sprint.ProjectId,
 UserId = userId,
     Status = SubmissionStatus.Draft
   };
            }

       return Ok(new ApiResponse<SprintSubmission>(true, submission, null, null));
    }
 catch (Exception ex)
    {
            _logger.LogError(ex, "Error fetching submission for sprint {SprintId}", sprintId);
  throw;
        }
    }

    /// <summary>
    /// Save/update the current user's submission for a sprint
    /// </summary>
    /// <param name="sprintId">Sprint ID</param>
    /// <param name="request">Submission data</param>
    /// <returns>Saved submission</returns>
    [HttpPost("sprint/{sprintId}")]
    [ProducesResponseType(typeof(ApiResponse<SprintSubmission>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintSubmission>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SprintSubmission>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SprintSubmission>>> SaveSubmission(
      string sprintId, 
[FromBody] SprintSubmissionRequestDto request)
    {
  if (string.IsNullOrWhiteSpace(sprintId))
        {
            return BadRequest(new ApiResponse<SprintSubmission>(false, null, "Sprint ID is required", null));
    }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                .ToList();
            // Log validation errors for easier debugging
            _logger.LogWarning("Submission validation failed for sprint {SprintId}: {Errors}", sprintId, string.Join(" | ", errors));
            return BadRequest(new ApiResponse<SprintSubmission>(false, null, "Validation failed", errors));
        }

        try
        {
      var userId = GetUserId();

   var sprint = await _sprintService.GetSprintByIdAsync(sprintId);
    if (sprint == null)
       {
       return NotFound(new ApiResponse<SprintSubmission>(false, null, "Sprint not found", null));
   }

   // Debug: log incoming request payload (trimmed) for easier diagnosis of 400s
   try
   {
       var reqJson = System.Text.Json.JsonSerializer.Serialize(request);
       _logger.LogDebug("SaveSubmission request payload for sprint {SprintId}: {Req}", sprintId, reqJson);
   }
   catch {}

   var submission = new SprintSubmission
            {
  SprintId = sprintId,
       ProjectId = sprint.ProjectId,
           UserId = userId,
     StoryPointsCompleted = request.StoryPointsCompleted,
       StoryPointsPlanned = request.StoryPointsPlanned,
     HoursWorked = request.HoursWorked,
              UserStories = request.UserStories?.Select(us => new UserStoryEntry
   {
         StoryId = (us.StoryId ?? string.Empty).Trim().Substring(0, Math.Min(50, (us.StoryId ?? string.Empty).Trim().Length)),
   Title = (us.Title ?? string.Empty).Trim().Substring(0, Math.Min(500, (us.Title ?? string.Empty).Trim().Length)),
     Description = (us.Description ?? string.Empty).Trim().Substring(0, Math.Min(2000, (us.Description ?? string.Empty).Trim().Length)),
   StoryPoints = Math.Clamp(us.StoryPoints, 0, 100),
Status = (us.Status ?? "Completed").Trim(),
       Remarks = (us.Remarks ?? string.Empty).Trim().Substring(0, Math.Min(1000, (us.Remarks ?? string.Empty).Trim().Length))
  }).Where(u => !string.IsNullOrWhiteSpace(u.StoryId) && !string.IsNullOrWhiteSpace(u.Title)).ToList() ?? new List<UserStoryEntry>(),
     FeaturesDelivered = request.FeaturesDelivered?.Select(f => new FeatureEntry
    {
   FeatureName = (f.FeatureName ?? string.Empty).Trim().Substring(0, Math.Min(500, (f.FeatureName ?? string.Empty).Trim().Length)),
              Description = (f.Description ?? string.Empty).Trim().Substring(0, Math.Min(2000, (f.Description ?? string.Empty).Trim().Length)),
       Module = (f.Module ?? string.Empty).Trim().Substring(0, Math.Min(200, (f.Module ?? string.Empty).Trim().Length)),
    Status = (f.Status ?? "Delivered").Trim()
         }).Where(f => !string.IsNullOrWhiteSpace(f.FeatureName)).ToList() ?? new List<FeatureEntry>(),
    Impediments = request.Impediments?.Select(i => new ImpedimentEntry
     {
       Description = (i.Description ?? string.Empty).Trim().Substring(0, Math.Min(2000, (i.Description ?? string.Empty).Trim().Length)),
         Category = (i.Category ?? "Technical").Trim().Substring(0, Math.Min(50, (i.Category ?? "Technical").Trim().Length)),
           Impact = (i.Impact ?? "Medium").Trim().Substring(0, Math.Min(50, (i.Impact ?? "Medium").Trim().Length)),
      Status = (i.Status ?? "Open").Trim().Substring(0, Math.Min(50, (i.Status ?? "Open").Trim().Length)),
               Resolution = (i.Resolution ?? string.Empty).Trim().Substring(0, Math.Min(2000, (i.Resolution ?? string.Empty).Trim().Length)),
          ReportedDate = i.ReportedDate ?? DateTime.UtcNow
 }).Where(im => !string.IsNullOrWhiteSpace(im.Description)).ToList() ?? new List<ImpedimentEntry>(),
 Appreciations = request.Appreciations?.Select(a => new AppreciationEntry
           {
     AppreciatedUserId = string.IsNullOrWhiteSpace(a.AppreciatedUserId) || (a.AppreciatedUserName ?? string.Empty).Trim() == "Team" ? null : a.AppreciatedUserId,
   AppreciatedUserName = (a.AppreciatedUserName ?? string.Empty).Trim().Substring(0, Math.Min(200, (a.AppreciatedUserName ?? string.Empty).Trim().Length)),
            Reason = (a.Reason ?? string.Empty).Trim().Substring(0, Math.Min(2000, (a.Reason ?? string.Empty).Trim().Length)),
          Category = (a.Category ?? "Teamwork").Trim().Substring(0, Math.Min(50, (a.Category ?? "Teamwork").Trim().Length))
 }).Where(ap => !string.IsNullOrWhiteSpace(ap.AppreciatedUserName) && !string.IsNullOrWhiteSpace(ap.Reason)).ToList() ?? new List<AppreciationEntry>(),
      Achievements = (request.Achievements ?? string.Empty).Trim().Substring(0, Math.Min(5000, (request.Achievements ?? string.Empty).Trim().Length)),
         Learnings = (request.Learnings ?? string.Empty).Trim().Substring(0, Math.Min(5000, (request.Learnings ?? string.Empty).Trim().Length)),
               NextSprintGoals = (request.NextSprintGoals ?? string.Empty).Trim().Substring(0, Math.Min(5000, (request.NextSprintGoals ?? string.Empty).Trim().Length)),
 AdditionalNotes = (request.AdditionalNotes ?? string.Empty).Trim().Substring(0, Math.Min(5000, (request.AdditionalNotes ?? string.Empty).Trim().Length)),
   Status = SubmissionStatus.Draft
     };

          var result = await _submissionService.CreateOrUpdateSubmissionAsync(submission, userId);
     _logger.LogInformation("Submission saved for user {UserId} sprint {SprintId}", userId, sprintId);
       return Ok(new ApiResponse<SprintSubmission>(true, result, "Submission saved successfully", null));
    }
    catch (BusinessRuleViolationException brv)
    {
        _logger.LogWarning(brv, "Business rule violated while saving submission for sprint {SprintId}: {Message}", sprintId, brv.Message);
        return BadRequest(new ApiResponse<SprintSubmission>(false, null, brv.Message, new List<string> { brv.ErrorCode }));
    }
  catch (Exception ex)
  {
   _logger.LogError(ex, "Error saving submission for sprint {SprintId}", sprintId);
        throw;
 }
    }

    /// <summary>
    /// Submit the submission (mark as submitted - cannot be edited after)
    /// </summary>
    /// <param name="submissionId">Submission ID</param>
    /// <returns>Submitted submission</returns>
    [HttpPost("{submissionId}/submit")]
    [ProducesResponseType(typeof(ApiResponse<SprintSubmission>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintSubmission>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SprintSubmission>>> SubmitSubmission(string submissionId)
    {
  if (string.IsNullOrWhiteSpace(submissionId))
        {
            return BadRequest(new ApiResponse<SprintSubmission>(false, null, "Submission ID is required", null));
       }

     try
        {
      var userId = GetUserId();

      _logger.LogDebug("Attempting to submit submission {SubmissionId} for user {UserId}", submissionId, userId);

            var result = await _submissionService.SubmitAsync(submissionId, userId);

          if (result == null)
  {
        return NotFound(new ApiResponse<SprintSubmission>(false, null, "Submission not found", null));
}

         _logger.LogInformation("Submission {SubmissionId} submitted by user {UserId}", submissionId, userId);
    return Ok(new ApiResponse<SprintSubmission>(true, result, "Submission submitted successfully", null));
    }
        catch (BusinessRuleViolationException brv)
        {
            // Return a structured ApiResponse so client gets consistent error payload
            _logger.LogWarning(brv, "Business rule violated while submitting {SubmissionId}: {Message}", submissionId, brv.Message);
            return BadRequest(new ApiResponse<SprintSubmission>(false, null, brv.Message, new List<string> { brv.ErrorCode }));
        }
        catch (Exception ex)
        {
    _logger.LogError(ex, "Error submitting submission {SubmissionId}", submissionId);
    throw;
  }
    }

    /// <summary>
    /// Reopen a submitted/reviewed submission for editing (sets status back to Draft)
    /// </summary>
    [HttpPost("{submissionId}/reopen")]
    [ProducesResponseType(typeof(ApiResponse<SprintSubmission>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintSubmission>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SprintSubmission>>> ReopenSubmission(string submissionId)
    {
        if (string.IsNullOrWhiteSpace(submissionId))
        {
            return BadRequest(new ApiResponse<SprintSubmission>(false, null, "Submission ID is required", null));
        }

        try
        {
            var userId = GetUserId();
            _logger.LogDebug("Attempting to reopen submission {SubmissionId} for user {UserId}", submissionId, userId);

            var result = await _submissionService.ReopenAsync(submissionId, userId);

            if (result == null)
            {
                return NotFound(new ApiResponse<SprintSubmission>(false, null, "Submission not found", null));
            }

            _logger.LogInformation("Submission {SubmissionId} reopened by user {UserId}", submissionId, userId);
            return Ok(new ApiResponse<SprintSubmission>(true, result, "Submission reopened for editing", null));
        }
        catch (BusinessRuleViolationException brv)
        {
            _logger.LogWarning(brv, "Business rule violated while reopening {SubmissionId}: {Message}", submissionId, brv.Message);
            return BadRequest(new ApiResponse<SprintSubmission>(false, null, brv.Message, new List<string> { brv.ErrorCode }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening submission {SubmissionId}", submissionId);
            throw;
        }
    }

    /// <summary>
    /// Get all submissions for a sprint (Manager only)
    /// </summary>
    /// <param name="sprintId">Sprint ID</param>
    /// <returns>List of submissions</returns>
    [HttpGet("sprint/{sprintId}/all")]
    [ProducesResponseType(typeof(ApiResponse<List<SprintSubmission>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SprintSubmission>>>> GetAllSprintSubmissions(string sprintId)
    {
        if (string.IsNullOrWhiteSpace(sprintId))
        {
    return BadRequest(new ApiResponse<List<SprintSubmission>>(false, null, "Sprint ID is required", null));
     }

        try
 {
       var submissions = await _submissionService.GetSprintSubmissionsAsync(sprintId);
           return Ok(new ApiResponse<List<SprintSubmission>>(true, submissions, null, null));
        }
        catch (Exception ex)
        {
    _logger.LogError(ex, "Error fetching all submissions for sprint {SprintId}", sprintId);
   throw;
    }
    }

    /// <summary>
    /// Get sprint report data for charts (Manager only)
    /// </summary>
    /// <param name="sprintId">Sprint ID</param>
    /// <returns>Sprint report data</returns>
    [HttpGet("sprint/{sprintId}/report")]
    [ProducesResponseType(typeof(ApiResponse<SprintReportData>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SprintReportData>>> GetSprintReport(string sprintId)
    {
  if (string.IsNullOrWhiteSpace(sprintId))
     {
      return BadRequest(new ApiResponse<SprintReportData>(false, null, "Sprint ID is required", null));
        }

   try
   {
   var reportData = await _submissionService.GetSprintReportDataAsync(sprintId);
       return Ok(new ApiResponse<SprintReportData>(true, reportData, null, null));
        }
 catch (Exception ex)
        {
  _logger.LogError(ex, "Error fetching report for sprint {SprintId}", sprintId);
      throw;
        }
    }

    /// <summary>
    /// Get all submissions by current user
    /// </summary>
    /// <returns>List of user's submissions</returns>
  [HttpGet("my-submissions")]
    [ProducesResponseType(typeof(ApiResponse<List<SprintSubmission>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SprintSubmission>>>> GetMySubmissions()
    {
        try
 {
   var userId = GetUserId();
  var submissions = await _submissionService.GetUserSubmissionsAsync(userId);
   return Ok(new ApiResponse<List<SprintSubmission>>(true, submissions, null, null));
        }
        catch (Exception ex)
     {
       _logger.LogError(ex, "Error fetching submissions for current user");
    throw;
        }
    }

    /// <summary>
    /// Delete a draft submission
    /// </summary>
    /// <param name="submissionId">Submission ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{submissionId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSubmission(string submissionId)
    {
        if (string.IsNullOrWhiteSpace(submissionId))
        {
      return BadRequest(new ApiResponse<bool>(false, false, "Submission ID is required", null));
     }

try
        {
var userId = GetUserId();
       var result = await _submissionService.DeleteSubmissionAsync(submissionId, userId);

            if (!result)
           {
return BadRequest(new ApiResponse<bool>(false, false, 
    "Cannot delete. Submission not found or already submitted.", null));
            }

         _logger.LogInformation("Submission {SubmissionId} deleted by user {UserId}", submissionId, userId);
         return Ok(new ApiResponse<bool>(true, true, "Submission deleted", null));
      }
        catch (Exception ex)
 {
    _logger.LogError(ex, "Error deleting submission {SubmissionId}", submissionId);
            throw;
        }
    }
}
