using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SprintTracker.Api.Models;
using SprintTracker.Api.Models.DTOs;
using SprintTracker.Api.Services;
using TaskStatus = SprintTracker.Api.Models.TaskStatus;

namespace SprintTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
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
    /// Create a new task
    /// </summary>
    /// <param name="request">Task creation details</param>
    /// <returns>Created task</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask([FromBody] CreateTaskRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                .ToList();
            return BadRequest(new ApiResponse<TaskDto>(false, null, "Validation failed", errors));
        }

        try
        {
            var createRequest = new CreateTaskRequest(
                request.ProjectId,
                request.SprintId,
                request.Title,
                request.Description,
                request.Type,
                request.Priority,
                request.StoryPoints,
                request.EstimatedHours,
                request.AssigneeId,
                request.ParentTaskId,
                request.Labels,
                request.AcceptanceCriteria,
                request.DueDate
            );

            var task = await _taskService.CreateTaskAsync(createRequest, GetUserId());
            _logger.LogInformation("Task created: {TaskKey} in project {ProjectId}", task.TaskKey, request.ProjectId);

            return CreatedAtAction(nameof(GetTask), new { id = task.Id },
                new ApiResponse<TaskDto>(true, task, "Task created successfully", null));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<TaskDto>(false, null, ex.Message, null));
        }
    }

    /// <summary>
    /// Get a task by ID
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>Task details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetTask(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiResponse<TaskDto>(false, null, "Task ID is required", null));
        }

        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null)
        {
            return NotFound(new ApiResponse<TaskDto>(false, null, "Task not found", null));
        }

        return Ok(new ApiResponse<TaskDto>(true, task, null, null));
    }

    /// <summary>
    /// Get tasks with optional filters
    /// </summary>
    /// <param name="projectId">Filter by project ID</param>
    /// <param name="sprintId">Filter by sprint ID</param>
    /// <param name="assigneeId">Filter by assignee ID</param>
    /// <param name="status">Filter by status</param>
    /// <param name="type">Filter by type</param>
    /// <param name="priority">Filter by priority</param>
    /// <param name="search">Search term</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of tasks</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<TaskDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<TaskDto>>>> GetTasks(
        [FromQuery] string? projectId,
        [FromQuery] string? sprintId,
        [FromQuery] string? assigneeId,
        [FromQuery] TaskStatus? status,
        [FromQuery] TaskType? type,
        [FromQuery] TaskPriority? priority,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filter = new TaskFilterOptions
        {
            ProjectId = projectId,
            SprintId = sprintId,
            AssigneeId = assigneeId,
            Status = status,
            Type = type,
            Priority = priority,
            SearchTerm = search?.Trim(),
            Page = page,
            PageSize = pageSize
        };

        var tasks = await _taskService.GetTasksAsync(filter);
        return Ok(new ApiResponse<PaginatedResponse<TaskDto>>(true, tasks, null, null));
    }

    /// <summary>
    /// Get backlog tasks for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>List of backlog tasks</returns>
    [HttpGet("backlog/{projectId}")]
    [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetBacklogTasks(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return BadRequest(new ApiResponse<List<TaskDto>>(false, null, "Project ID is required", null));
        }

        var tasks = await _taskService.GetBacklogTasksAsync(projectId);
        return Ok(new ApiResponse<List<TaskDto>>(true, tasks, null, null));
    }

    /// <summary>
    /// Get tasks for a sprint
    /// </summary>
    /// <param name="sprintId">Sprint ID</param>
    /// <returns>List of sprint tasks</returns>
    [HttpGet("sprint/{sprintId}")]
    [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetSprintTasks(string sprintId)
    {
        if (string.IsNullOrWhiteSpace(sprintId))
        {
            return BadRequest(new ApiResponse<List<TaskDto>>(false, null, "Sprint ID is required", null));
        }

        var tasks = await _taskService.GetSprintTasksAsync(sprintId);
        return Ok(new ApiResponse<List<TaskDto>>(true, tasks, null, null));
    }

    /// <summary>
    /// Get tasks assigned to the current user
    /// </summary>
    /// <returns>List of user's tasks</returns>
    [HttpGet("my-tasks")]
    [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetMyTasks()
    {
        var tasks = await _taskService.GetMyTasksAsync(GetUserId());
        return Ok(new ApiResponse<List<TaskDto>>(true, tasks, null, null));
    }

    /// <summary>
    /// Update an existing task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="request">Task update details</param>
    /// <returns>Updated task</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(string id, [FromBody] UpdateTaskRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiResponse<TaskDto>(false, null, "Task ID is required", null));
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                .ToList();
            return BadRequest(new ApiResponse<TaskDto>(false, null, "Validation failed", errors));
        }

        var updateRequest = new UpdateTaskRequest(
            request.Title,
            request.Description,
            request.Type,
            request.Status,
            request.Priority,
            request.StoryPoints,
            request.EstimatedHours,
            request.RemainingHours,
            request.AssigneeId,
            request.SprintId,
            request.Labels,
            request.DueDate,
            request.Order
        );

        var task = await _taskService.UpdateTaskAsync(id, updateRequest, GetUserId());
        if (task == null)
        {
            return NotFound(new ApiResponse<TaskDto>(false, null, "Task not found", null));
        }

        _logger.LogInformation("Task updated: {TaskId} by user {UserId}", id, GetUserId());
        return Ok(new ApiResponse<TaskDto>(true, task, "Task updated successfully", null));
    }

    /// <summary>
    /// Update task status
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="status">New status</param>
    /// <returns>Updated task</returns>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTaskStatus(string id, [FromBody] TaskStatus status)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiResponse<TaskDto>(false, null, "Task ID is required", null));
        }

        var task = await _taskService.UpdateTaskStatusAsync(id, status, GetUserId());
        if (task == null)
        {
            return NotFound(new ApiResponse<TaskDto>(false, null, "Task not found", null));
        }

        _logger.LogInformation("Task status updated: {TaskId} to {Status}", id, status);
        return Ok(new ApiResponse<TaskDto>(true, task, "Task status updated", null));
    }

    /// <summary>
    /// Move task to another sprint or position
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="request">Move details</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/move")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> MoveTask(string id, [FromBody] MoveTaskRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiResponse<bool>(false, false, "Task ID is required", null));
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                .ToList();
            return BadRequest(new ApiResponse<bool>(false, false, "Validation failed", errors));
        }

        var moveRequest = new MoveTaskRequest(request.SprintId, request.Order);
        var result = await _taskService.MoveTaskAsync(id, moveRequest, GetUserId());
        if (!result)
        {
            return BadRequest(new ApiResponse<bool>(false, false, "Failed to move task", null));
        }

        _logger.LogInformation("Task moved: {TaskId} to sprint {SprintId}", id, request.SprintId ?? "backlog");
        return Ok(new ApiResponse<bool>(true, true, "Task moved successfully", null));
    }

    /// <summary>
    /// Log time spent on a task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="request">Time log details</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/log-time")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> LogTime(string id, [FromBody] LogTimeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiResponse<bool>(false, false, "Task ID is required", null));
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                .ToList();
            return BadRequest(new ApiResponse<bool>(false, false, "Validation failed", errors));
        }

        var logRequest = new LogTimeRequest(request.Hours, request.Description);
        var result = await _taskService.LogTimeAsync(id, logRequest, GetUserId());
        if (!result)
        {
            return NotFound(new ApiResponse<bool>(false, false, "Task not found", null));
        }

        _logger.LogInformation("Time logged: {Hours}h on task {TaskId} by user {UserId}", request.Hours, id, GetUserId());
        return Ok(new ApiResponse<bool>(true, true, "Time logged successfully", null));
    }

    /// <summary>
    /// Delete a task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiResponse<bool>(false, false, "Task ID is required", null));
        }

        var result = await _taskService.DeleteTaskAsync(id, GetUserId());
        if (!result)
        {
            return NotFound(new ApiResponse<bool>(false, false, "Task not found", null));
        }

        _logger.LogInformation("Task deleted: {TaskId} by user {UserId}", id, GetUserId());
        return Ok(new ApiResponse<bool>(true, true, "Task deleted successfully", null));
    }
}
