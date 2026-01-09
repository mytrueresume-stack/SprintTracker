using System.Net;
using System.Text.Json;
using SprintTracker.Api.Exceptions;
using SprintTracker.Api.Models.DTOs;

namespace SprintTracker.Api.Middleware;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions
/// and returns a consistent error response format.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
     IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
   await _next(context);
        }
        catch (Exception ex)
        {
  await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
   var (statusCode, message, errors, errorCode) = MapException(exception);

        // Log the exception with appropriate level
 LogException(exception, statusCode, context.TraceIdentifier);

    context.Response.ContentType = "application/json";
      context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors,
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        // If an error code is present but no errors list, include it for client consistency
        if (response.Errors == null && !string.IsNullOrWhiteSpace(response.ErrorCode))
        {
            response.Errors = new List<string> { response.ErrorCode };
        }

        // Include stack trace only in development
        if (_environment.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
        {
            response.DeveloperMessage = exception.ToString();
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }

    private (HttpStatusCode statusCode, string message, List<string>? errors, string? errorCode) MapException(Exception exception)
    {
        return exception switch
   {
            // Custom domain exceptions
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
    notFound.Message,
         null,
    notFound.ErrorCode),

   BusinessRuleViolationException businessRule => (
      HttpStatusCode.BadRequest,
         businessRule.Message,
          null,
    businessRule.ErrorCode),

    ForbiddenException forbidden => (
         HttpStatusCode.Forbidden,
    forbidden.Message,
null,
   forbidden.ErrorCode),

   ValidationException validation => (
         HttpStatusCode.BadRequest,
      validation.Message,
      validation.ValidationErrors.SelectMany(e => e.Value).ToList(),
        validation.ErrorCode),

      DuplicateResourceException duplicate => (
                HttpStatusCode.Conflict,
                duplicate.Message,
    null,
             duplicate.ErrorCode),

            ConflictException conflict => (
        HttpStatusCode.Conflict,
         conflict.Message,
  null,
     conflict.ErrorCode),

       ServiceUnavailableException serviceUnavailable => (
             HttpStatusCode.ServiceUnavailable,
                serviceUnavailable.Message,
 null,
 serviceUnavailable.ErrorCode),

     // Built-in exceptions
            UnauthorizedAccessException => (
   HttpStatusCode.Unauthorized,
    "You are not authorized to perform this action.",
            null,
  "UNAUTHORIZED"),

            ArgumentNullException argNull => (
     HttpStatusCode.BadRequest,
    $"Required parameter '{argNull.ParamName}' was not provided.",
          null,
     "MISSING_PARAMETER"),

ArgumentException argEx => (
        HttpStatusCode.BadRequest,
    argEx.Message,
        null,
      "INVALID_ARGUMENT"),

      InvalidOperationException invalidOp => (
       HttpStatusCode.BadRequest,
        invalidOp.Message,
      null,
      "INVALID_OPERATION"),

      // Malformed request body (e.g., invalid JSON)
      Microsoft.AspNetCore.Http.BadHttpRequestException badReq => (
        HttpStatusCode.BadRequest,
        badReq.Message,
        null,
        "BAD_REQUEST"),

      OperationCanceledException => (
              HttpStatusCode.RequestTimeout,
      "The operation was cancelled or timed out.",
           null,
     "OPERATION_CANCELLED"),

  // MongoDB exceptions
     MongoDB.Driver.MongoConnectionException => (
    HttpStatusCode.ServiceUnavailable,
"Database connection failed. Please try again later.",
                null,
    "DATABASE_CONNECTION_ERROR"),

          MongoDB.Driver.MongoWriteException writeEx when writeEx.WriteError?.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey => (
          HttpStatusCode.Conflict,
      "A record with this identifier already exists.",
        null,
       "DUPLICATE_KEY"),

            MongoDB.Driver.MongoException => (
    HttpStatusCode.InternalServerError,
        "A database error occurred. Please try again later.",
     null,
              "DATABASE_ERROR"),

          // Default fallback
          _ => (
    HttpStatusCode.InternalServerError,
         "An unexpected error occurred. Please try again later.",
     null,
                "INTERNAL_ERROR")
 };
    }

    private void LogException(Exception exception, HttpStatusCode statusCode, string traceId)
    {
        var logLevel = statusCode switch
        {
            HttpStatusCode.InternalServerError => LogLevel.Error,
            HttpStatusCode.ServiceUnavailable => LogLevel.Error,
          HttpStatusCode.BadRequest => LogLevel.Warning,
          HttpStatusCode.Unauthorized => LogLevel.Warning,
    HttpStatusCode.Forbidden => LogLevel.Warning,
       HttpStatusCode.NotFound => LogLevel.Information,
     _ => LogLevel.Warning
    };

        _logger.Log(
            logLevel,
         exception,
        "Exception occurred. StatusCode: {StatusCode}, TraceId: {TraceId}, Type: {ExceptionType}",
(int)statusCode,
            traceId,
        exception.GetType().Name);
    }
}

/// <summary>
/// Standardized error response format
/// </summary>
public class ErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public string? ErrorCode { get; set; }
    public List<string>? Errors { get; set; }
public string? TraceId { get; set; }
  public DateTime Timestamp { get; set; }
    public string? DeveloperMessage { get; set; }
}

/// <summary>
/// Extension method to register the global exception handler middleware
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
