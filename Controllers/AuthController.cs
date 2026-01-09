using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SprintTracker.Api.Models;
using SprintTracker.Api.Models.DTOs;
using SprintTracker.Api.Services;

namespace SprintTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequestDto request)
    {
        // Read raw body for diagnostics and fallback binding
        string rawBody = string.Empty;
        try
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            rawBody = await reader.ReadToEndAsync();
            Request.Body.Position =0;
            _logger.LogDebug("Raw registration request body: {RawBody}", rawBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to read raw request body for registration");
        }

        // If model binding failed (ModelState invalid), attempt to deserialize manually from raw JSON (tolerant)
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Initial model binding failed. Attempting manual deserialization.");
            try
            {
                if (!string.IsNullOrWhiteSpace(rawBody))
                {
                    var opts = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var manual = JsonSerializer.Deserialize<RegisterRequestDto>(rawBody, opts);
                    if (manual != null)
                    {
                        // Replace request and re-validate
                        request = manual;
                        // Clear existing modelstate and re-validate
                        ModelState.Clear();
                        TryValidateModel(request);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Manual deserialization of registration payload failed");
            }

            // Additional tolerant parsing: extract "role" from raw JSON if present (handle numeric or string)
            try
            {
                if (!string.IsNullOrWhiteSpace(rawBody))
                {
                    using var doc = JsonDocument.Parse(rawBody);
                    if (doc.RootElement.TryGetProperty("role", out var roleProp))
                    {
                        if (roleProp.ValueKind == JsonValueKind.Number && roleProp.TryGetInt32(out var roleInt))
                        {
                            request = request with { Role = (UserRole)roleInt };
                            ModelState.Clear();
                            TryValidateModel(request);
                        }
                        else if (roleProp.ValueKind == JsonValueKind.String)
                        {
                            var roleStr = roleProp.GetString();
                            if (!string.IsNullOrEmpty(roleStr))
                            {
                                if (int.TryParse(roleStr, out var rInt))
                                {
                                    request = request with { Role = (UserRole)rInt };
                                    ModelState.Clear();
                                    TryValidateModel(request);
                                }
                                else if (Enum.TryParse<UserRole>(roleStr, true, out var parsed))
                                {
                                    request = request with { Role = parsed };
                                    ModelState.Clear();
                                    TryValidateModel(request);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to robustly parse role from registration payload");
            }
        }

        _logger.LogInformation("Registration attempt for email: {Email}, FirstName: {FirstName}, LastName: {LastName}, Role: {Role}", 
            request?.Email, request?.FirstName, request?.LastName, request?.Role);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                .ToList();
            _logger.LogWarning("Registration validation failed for email {Email}: {Errors}", request?.Email, string.Join(", ", errors));

            var apiResponse = new ApiResponse<AuthResponse>(false, null, "Validation failed", errors);
            return new JsonResult(apiResponse) { StatusCode = StatusCodes.Status400BadRequest, ContentType = "application/json" };
        }

        // Map DTO to domain request
        var registerRequest = new RegisterRequest(
            request!.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Role
        );

        var result = await _authService.RegisterAsync(registerRequest);

        if (result == null)
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
            var apiResponse = new ApiResponse<AuthResponse>(false, null, "An account with this email already exists", new List<string> { "Email already exists" });
            return new JsonResult(apiResponse) { StatusCode = StatusCodes.Status400BadRequest, ContentType = "application/json" };
        }

        _logger.LogInformation("User registered successfully: {Email}", request.Email);
        var successResponse = new ApiResponse<AuthResponse>(true, result, "Registration successful", null);
        return new JsonResult(successResponse) { StatusCode = StatusCodes.Status200OK, ContentType = "application/json" };
    }

    /// <summary>
    /// Authenticate a user and get JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                .ToList();
            return BadRequest(new ApiResponse<AuthResponse>(false, null, "Validation failed", errors));
        }

        var loginRequest = new LoginRequest(request.Email, request.Password);
        var result = await _authService.LoginAsync(loginRequest);

        if (result == null)
        {
            _logger.LogWarning("Login failed for email: {Email}", request.Email);
            return Unauthorized(new ApiResponse<AuthResponse>(false, null, "Invalid email or password", null));
        }

        _logger.LogInformation("User logged in: {Email}", request.Email);
        return Ok(new ApiResponse<AuthResponse>(true, result, "Login successful", null));
    }

    /// <summary>
    /// Get current authenticated user's information
    /// </summary>
    /// <returns>User details</returns>
    [HttpGet("me")]
    [Authorize]
    [DisableRateLimiting]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ApiResponse<UserDto>(false, null, "User not authenticated", null));
        }

        var user = await _authService.GetCurrentUserAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Authenticated user not found in database: {UserId}", userId);
            return NotFound(new ApiResponse<UserDto>(false, null, "User not found", null));
        }

        return Ok(new ApiResponse<UserDto>(true, user, null, null));
    }

    /// <summary>
    /// Refresh the authentication token
    /// </summary>
    /// <returns>New authentication response with refreshed JWT token</returns>
    [HttpPost("refresh")]
    [Authorize]
    [DisableRateLimiting]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ApiResponse<AuthResponse>(false, null, "User not authenticated", null));
        }

        var user = await _authService.GetCurrentUserAsync(userId);
        if (user == null)
        {
            return Unauthorized(new ApiResponse<AuthResponse>(false, null, "User not found", null));
        }

        // Note: In a production system, you would want to implement proper token refresh logic
        // For now, we'll just return a success message indicating the user should re-login
        return Ok(new ApiResponse<AuthResponse>(false, null, "Please re-authenticate to get a new token", null));
    }
}
