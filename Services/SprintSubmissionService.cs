using MongoDB.Driver;
using MongoDB.Bson;
using SprintTracker.Api.Data;
using SprintTracker.Api.Exceptions;
using SprintTracker.Api.Models;

namespace SprintTracker.Api.Services;

public interface ISprintSubmissionService
{
    Task<SprintSubmission> CreateOrUpdateSubmissionAsync(SprintSubmission submission, string userId);
    Task<SprintSubmission?> GetSubmissionAsync(string sprintId, string userId);
    Task<SprintSubmission?> GetSubmissionByIdAsync(string submissionId);
    Task<List<SprintSubmission>> GetSprintSubmissionsAsync(string sprintId);
    Task<List<SprintSubmission>> GetUserSubmissionsAsync(string userId);
    Task<SprintSubmission?> SubmitAsync(string submissionId, string userId);
    Task<SprintSubmission?> ReopenAsync(string submissionId, string userId);
    Task<bool> DeleteSubmissionAsync(string submissionId, string userId);
    Task<SprintReportData> GetSprintReportDataAsync(string sprintId);
}

public class SprintSubmissionService : ISprintSubmissionService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<SprintSubmissionService> _logger;

    public SprintSubmissionService(MongoDbContext context, ILogger<SprintSubmissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SprintSubmission> CreateOrUpdateSubmissionAsync(SprintSubmission submission, string userId)
    {
        if (submission == null) throw new ArgumentNullException(nameof(submission));
        if (string.IsNullOrWhiteSpace(userId)) throw new UnauthorizedAccessException("User is not authenticated");
        if (string.IsNullOrWhiteSpace(submission.SprintId)) throw new BusinessRuleViolationException("Sprint ID is required", "SPRINT_REQUIRED");

        try
        {
            // Ensure the sprint exists
            var sprint = await _context.Sprints.Find(s => s.Id == submission.SprintId).FirstOrDefaultAsync();
            if (sprint == null) throw new BusinessRuleViolationException("Sprint not found", "SPRINT_NOT_FOUND");

            // Defensive defaults
            submission.UserStories ??= new List<UserStoryEntry>();
            submission.FeaturesDelivered ??= new List<FeatureEntry>();
            submission.Impediments ??= new List<ImpedimentEntry>();
            submission.Appreciations ??= new List<AppreciationEntry>();

            // Normalize and validate points
            if (submission.StoryPointsPlanned <= 0 && submission.UserStories.Any())
            {
                var plannedFromStories = submission.UserStories.Sum(us => Math.Max(us.StoryPoints, 0));
                submission.StoryPointsPlanned = plannedFromStories;
                _logger.LogInformation("Computed StoryPointsPlanned from user stories for submission {SubmissionId}: {Planned}", submission.Id, plannedFromStories);
            }

            submission.StoryPointsPlanned = Math.Max(submission.StoryPointsPlanned, 0);
            submission.StoryPointsCompleted = Math.Max(submission.StoryPointsCompleted, 0);

            if (submission.StoryPointsCompleted > submission.StoryPointsPlanned)
            {
                throw new BusinessRuleViolationException("Story points completed cannot exceed planned points", "INVALID_POINTS");
            }

            // Check if submission already exists for this user and sprint
            var existing = await _context.SprintSubmissions
                .Find(s => s.SprintId == submission.SprintId && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                // Cannot update submitted submissions
                if (existing.Status == SubmissionStatus.Submitted)
                {
                    throw new BusinessRuleViolationException(
                        "Cannot update a submission that has already been submitted",
                        "SUBMISSION_ALREADY_SUBMITTED");
                }

                // Update existing
                submission.Id = existing.Id;
                submission.CreatedAt = existing.CreatedAt;
                submission.UpdatedAt = DateTime.UtcNow;
                submission.UserId = userId;

                // Diagnostic log: capture points and counts to help debug dashboard progression
                _logger.LogInformation("Updating submission {SubmissionId} for sprint {SprintId} user {UserId}: Planned={Planned}, Completed={Completed}, Stories={Stories}, Features={Features}, Impediments={Impediments}, Appreciations={Appreciations}",
                    submission.Id, submission.SprintId, userId,
                    submission.StoryPointsPlanned, submission.StoryPointsCompleted,
                    submission.UserStories.Count, submission.FeaturesDelivered.Count,
                    submission.Impediments.Count, submission.Appreciations.Count);

                await _context.SprintSubmissions.ReplaceOneAsync(
                    s => s.Id == existing.Id,
                    submission
                );

                _logger.LogInformation("Sprint submission updated for user {UserId} sprint {SprintId}",
                    userId, submission.SprintId);
                return submission;
            }
            else
            {
                // Create new
                submission.UserId = userId;
                submission.CreatedAt = DateTime.UtcNow;
                submission.UpdatedAt = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(submission.Id)) submission.Id = ObjectId.GenerateNewId().ToString();
                submission.ProjectId ??= sprint.ProjectId;

                // Diagnostic log: new submission details
                _logger.LogInformation("Creating submission for sprint {SprintId} user {UserId}: Planned={Planned}, Completed={Completed}, Stories={Stories}, Features={Features}, Impediments={Impediments}, Appreciations={Appreciations}",
                    submission.SprintId, userId,
                    submission.StoryPointsPlanned, submission.StoryPointsCompleted,
                    submission.UserStories.Count, submission.FeaturesDelivered.Count,
                    submission.Impediments.Count, submission.Appreciations.Count);

                await _context.SprintSubmissions.InsertOneAsync(submission);
                _logger.LogInformation("Sprint submission created for user {UserId} sprint {SprintId}",
                    userId, submission.SprintId);
                return submission;
            }
        }
        catch (BusinessRuleViolationException)
        {
            throw;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Database error saving submission for sprint {SprintId}", submission.SprintId);
            throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<SprintSubmission?> GetSubmissionAsync(string sprintId, string userId)
    {
        try
  {
   return await _context.SprintSubmissions
       .Find(s => s.SprintId == sprintId && s.UserId == userId)
  .FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
    _logger.LogError(ex, "Database error fetching submission for sprint {SprintId} user {UserId}", sprintId, userId);
            throw new ServiceUnavailableException("Database", ex);
      }
    }

  public async Task<SprintSubmission?> GetSubmissionByIdAsync(string submissionId)
    {
   try
  {
    return await _context.SprintSubmissions
.Find(s => s.Id == submissionId)
     .FirstOrDefaultAsync();
        }
    catch (MongoException ex)
{
   _logger.LogError(ex, "Database error fetching submission {SubmissionId}", submissionId);
     throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<List<SprintSubmission>> GetSprintSubmissionsAsync(string sprintId)
    {
try
        {
  return await _context.SprintSubmissions
   .Find(s => s.SprintId == sprintId)
    .ToListAsync();
        }
 catch (MongoException ex)
     {
  _logger.LogError(ex, "Database error fetching submissions for sprint {SprintId}", sprintId);
       throw new ServiceUnavailableException("Database", ex);
        }
    }

  public async Task<List<SprintSubmission>> GetUserSubmissionsAsync(string userId)
    {
        try
{
         return await _context.SprintSubmissions
  .Find(s => s.UserId == userId)
   .SortByDescending(s => s.CreatedAt)
   .ToListAsync();
   }
      catch (MongoException ex)
        {
  _logger.LogError(ex, "Database error fetching submissions for user {UserId}", userId);
       throw new ServiceUnavailableException("Database", ex);
        }
    }

    public async Task<SprintSubmission?> SubmitAsync(string submissionId, string userId)
    {
 try
    {
            var submission = await _context.SprintSubmissions
  .Find(s => s.Id == submissionId && s.UserId == userId)
.FirstOrDefaultAsync();

   if (submission == null) return null;

  if (submission.Status == SubmissionStatus.Submitted)
            {
      throw new BusinessRuleViolationException(
    "This submission has already been submitted", 
  "ALREADY_SUBMITTED");
          }

  // Normalize and validate before submit
  submission.UserStories ??= new List<UserStoryEntry>();
  if (submission.StoryPointsPlanned <= 0 && submission.UserStories.Any())
  {
      submission.StoryPointsPlanned = submission.UserStories.Sum(us => Math.Max(us.StoryPoints, 0));
  }

  if (submission.StoryPointsCompleted > submission.StoryPointsPlanned)
  {
      throw new BusinessRuleViolationException("Cannot submit: completed story points exceed planned points", "INVALID_POINTS");
  }

   var update = Builders<SprintSubmission>.Update
    .Set(s => s.Status, SubmissionStatus.Submitted)
     .Set(s => s.SubmittedAt, DateTime.UtcNow)
   .Set(s => s.UpdatedAt, DateTime.UtcNow);

    // Diagnostic log: snapshot before marking submitted
    _logger.LogInformation("Submitting submission {SubmissionId} for user {UserId}: Planned={Planned}, Completed={Completed}, Stories={Stories}, Features={Features}, Impediments={Impediments}, Appreciations={Appreciations}",
        submissionId, userId,
        submission.StoryPointsPlanned, submission.StoryPointsCompleted,
        submission.UserStories?.Count ?? 0, submission.FeaturesDelivered?.Count ?? 0,
        submission.Impediments?.Count ?? 0, submission.Appreciations?.Count ?? 0);

    await _context.SprintSubmissions.UpdateOneAsync(s => s.Id == submissionId, update);

       submission.Status = SubmissionStatus.Submitted;
   submission.SubmittedAt = DateTime.UtcNow;

       _logger.LogInformation("Sprint submission {SubmissionId} submitted by user {UserId}", submissionId, userId);
return submission;
        }
        catch (BusinessRuleViolationException)
        {
       throw;
        }
        catch (MongoException ex)
    {
         _logger.LogError(ex, "Database error submitting submission {SubmissionId}", submissionId);
   throw new ServiceUnavailableException("Database", ex);
        }
}

    public async Task<SprintSubmission?> ReopenAsync(string submissionId, string userId)
    {
        try
        {
            var submission = await _context.SprintSubmissions
                .Find(s => s.Id == submissionId && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (submission == null) return null;

            if (submission.Status != SubmissionStatus.Submitted && submission.Status != SubmissionStatus.Reviewed)
            {
                throw new BusinessRuleViolationException("Only submitted or reviewed submissions can be reopened", "CANNOT_REOPEN");
            }

            var update = Builders<SprintSubmission>.Update
                .Set(s => s.Status, SubmissionStatus.Draft)
                .Set(s => s.SubmittedAt, null)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _context.SprintSubmissions.UpdateOneAsync(s => s.Id == submissionId, update);

            submission.Status = SubmissionStatus.Draft;
            submission.SubmittedAt = null;
            submission.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Sprint submission {SubmissionId} reopened by user {UserId}", submissionId, userId);
            return submission;
        }
        catch (BusinessRuleViolationException)
        {
            throw;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Database error reopening submission {SubmissionId}", submissionId);
            throw new ServiceUnavailableException("Database", ex);
        }
    }

  public async Task<bool> DeleteSubmissionAsync(string submissionId, string userId)
 {
   try
        {
     var result = await _context.SprintSubmissions
   .DeleteOneAsync(s => s.Id == submissionId && s.UserId == userId && s.Status == SubmissionStatus.Draft);
  
         if (result.DeletedCount > 0)
      {
    _logger.LogInformation("Sprint submission {SubmissionId} deleted by user {UserId}", submissionId, userId);
       }

return result.DeletedCount > 0;
   }
  catch (MongoException ex)
        {
  _logger.LogError(ex, "Database error deleting submission {SubmissionId}", submissionId);
     throw new ServiceUnavailableException("Database", ex);
   }
 }

    public async Task<SprintReportData> GetSprintReportDataAsync(string sprintId)
    {
        try
        {
            var sprint = await _context.Sprints.Find(s => s.Id == sprintId).FirstOrDefaultAsync();
            var submissions = await GetSprintSubmissionsAsync(sprintId) ?? new List<SprintSubmission>();
            var users = await _context.Users.Find(_ => true).ToListAsync();

            var userDict = users.ToDictionary(u => u.Id, u => u);

            // Aggregate data (null-safe)
            var totalStoryPointsPlanned = submissions.Sum(s => s?.StoryPointsPlanned ?? 0);
            var totalStoryPointsCompleted = submissions.Sum(s => s?.StoryPointsCompleted ?? 0);
            var totalHoursWorked = submissions.Sum(s => s?.HoursWorked ?? 0);
            var totalUserStories = submissions.SelectMany(s => s?.UserStories ?? Enumerable.Empty<UserStoryEntry>()).Count();
            var totalFeatures = submissions.SelectMany(s => s?.FeaturesDelivered ?? Enumerable.Empty<FeatureEntry>()).Count();
            var totalImpediments = submissions.SelectMany(s => s?.Impediments ?? Enumerable.Empty<ImpedimentEntry>()).Count();
            var totalAppreciations = submissions.SelectMany(s => s?.Appreciations ?? Enumerable.Empty<AppreciationEntry>()).Count();

            // Distinct team members who submitted
            var totalTeamMembers = submissions.Select(s => s.UserId).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Count();

            // Per user breakdown (aggregate across multiple submissions by same user)
            var userBreakdown = submissions
                .GroupBy(s => s.UserId)
                .Select(g => new UserSprintSummary
                {
                    UserId = g.Key,
                    UserName = userDict.TryGetValue(g.Key, out var u) ? u.FullName : "Unknown",
                    StoryPointsPlanned = g.Sum(x => x.StoryPointsPlanned),
                    StoryPointsCompleted = g.Sum(x => x.StoryPointsCompleted),
                    HoursWorked = g.Sum(x => x.HoursWorked),
                    UserStoriesCount = g.Sum(x => (x.UserStories ?? Enumerable.Empty<UserStoryEntry>()).Count()),
                    FeaturesCount = g.Sum(x => (x.FeaturesDelivered ?? Enumerable.Empty<FeatureEntry>()).Count()),
                    ImpedimentsCount = g.Sum(x => (x.Impediments ?? Enumerable.Empty<ImpedimentEntry>()).Count()),
                    AppreciationsGiven = g.Sum(x => (x.Appreciations ?? Enumerable.Empty<AppreciationEntry>()).Count()),
                    SubmissionStatus = string.Join(",", g.Select(x => x.Status.ToString()).Distinct())
                }).ToList();

            // All user stories (null-safe)
            var allUserStories = submissions
    .SelectMany(s => (s.UserStories ?? Enumerable.Empty<UserStoryEntry>()).Select(us => new UserStoryReport
     {
 StoryId = us.StoryId,
    Title = us.Title,
          StoryPoints = us.StoryPoints,
 Status = us.Status,
      ReportedBy = userDict.TryGetValue(s.UserId, out var u) ? u.FullName : "Unknown"
   })).ToList();

            // All features (null-safe)
    var allFeatures = submissions
   .SelectMany(s => (s.FeaturesDelivered ?? Enumerable.Empty<FeatureEntry>()).Select(f => new FeatureReport
 {
     FeatureName = f.FeatureName,
 Description = f.Description,
    Module = f.Module,
        Status = f.Status,
          DeliveredBy = userDict.TryGetValue(s.UserId, out var u) ? u.FullName : "Unknown"
         })).ToList();

      // All impediments (null-safe)
       var allImpediments = submissions
    .SelectMany(s => (s.Impediments ?? Enumerable.Empty<ImpedimentEntry>()).Select(i => new ImpedimentReport
  {
 Description = i.Description,
 Category = i.Category,
   Impact = i.Impact,
           Status = i.Status,
     Resolution = i.Resolution,
  ReportedBy = userDict.TryGetValue(s.UserId, out var u) ? u.FullName : "Unknown",
       ReportedDate = i.ReportedDate
     })).ToList();

            // All appreciations (null-safe)
       var allAppreciations = submissions
  .SelectMany(s => (s.Appreciations ?? Enumerable.Empty<AppreciationEntry>()).Select(a => new AppreciationReport
          {
AppreciatedUserName = a.AppreciatedUserName,
    Reason = a.Reason,
       Category = a.Category,
         GivenBy = userDict.TryGetValue(s.UserId, out var u) ? u.FullName : "Unknown"
       })).ToList();

      return new SprintReportData
    {
    SprintId = sprintId,
   SprintName = sprint?.Name ?? "Unknown",
         SprintNumber = sprint?.SprintNumber ?? 0,
      StartDate = sprint?.StartDate ?? DateTime.MinValue,
  EndDate = sprint?.EndDate ?? DateTime.MinValue,
  TotalTeamMembers = totalTeamMembers,
   TotalStoryPointsPlanned = totalStoryPointsPlanned,
  TotalStoryPointsCompleted = totalStoryPointsCompleted,
  CompletionPercentage = totalStoryPointsPlanned > 0
      ? Math.Round((decimal)totalStoryPointsCompleted / totalStoryPointsPlanned * 100, 1)
      : 0,
       TotalHoursWorked = totalHoursWorked,
        TotalUserStories = totalUserStories,
     TotalFeatures = totalFeatures,
    TotalImpediments = totalImpediments,
    OpenImpediments = allImpediments.Count(i => string.Equals(i.Status, "Open", StringComparison.OrdinalIgnoreCase)),
   TotalAppreciations = totalAppreciations,
       UserBreakdown = userBreakdown,
 UserStories = allUserStories,
 Features = allFeatures,
    Impediments = allImpediments,
    Appreciations = allAppreciations
       };
     }
        catch (MongoException ex)
        {
   _logger.LogError(ex, "Database error fetching report for sprint {SprintId}", sprintId);
       throw new ServiceUnavailableException("Database", ex);
        }
    }
}

// Report DTOs
public class SprintReportData
{
    public string SprintId { get; set; } = null!;
    public string SprintName { get; set; } = null!;
    public int SprintNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalTeamMembers { get; set; }
    public int TotalStoryPointsPlanned { get; set; }
    public int TotalStoryPointsCompleted { get; set; }
    public decimal CompletionPercentage { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public int TotalUserStories { get; set; }
    public int TotalFeatures { get; set; }
    public int TotalImpediments { get; set; }
    public int OpenImpediments { get; set; }
    public int TotalAppreciations { get; set; }
    public List<UserSprintSummary> UserBreakdown { get; set; } = new();
    public List<UserStoryReport> UserStories { get; set; } = new();
    public List<FeatureReport> Features { get; set; } = new();
    public List<ImpedimentReport> Impediments { get; set; } = new();
    public List<AppreciationReport> Appreciations { get; set; } = new();
}

public class UserSprintSummary
{
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public int StoryPointsPlanned { get; set; }
    public int StoryPointsCompleted { get; set; }
    public decimal HoursWorked { get; set; }
    public int UserStoriesCount { get; set; }
    public int FeaturesCount { get; set; }
    public int ImpedimentsCount { get; set; }
    public int AppreciationsGiven { get; set; }
    public string SubmissionStatus { get; set; } = null!;
}

public class UserStoryReport
{
    public string StoryId { get; set; } = null!;
  public string Title { get; set; } = null!;
    public int StoryPoints { get; set; }
    public string Status { get; set; } = null!;
    public string ReportedBy { get; set; } = null!;
}

public class FeatureReport
{
    public string FeatureName { get; set; } = null!;
    public string? Description { get; set; }
    public string? Module { get; set; }
    public string Status { get; set; } = null!;
    public string DeliveredBy { get; set; } = null!;
}

public class ImpedimentReport
{
    public string Description { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Impact { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Resolution { get; set; }
    public string ReportedBy { get; set; } = null!;
    public DateTime ReportedDate { get; set; }
}

public class AppreciationReport
{
    public string AppreciatedUserName { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string GivenBy { get; set; } = null!;
}
