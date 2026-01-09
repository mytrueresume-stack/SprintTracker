using System.Diagnostics;

namespace SprintTracker.Api.Middleware;

/// <summary>
/// Middleware to log request and response details for better observability
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
    _next = next;
    _logger = logger;
    }

 public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
      var requestId = context.TraceIdentifier;

    // Log request
        _logger.LogInformation(
      "HTTP {Method} {Path} started. RequestId: {RequestId}, User: {User}",
       context.Request.Method,
        context.Request.Path,
       requestId,
      context.User?.Identity?.Name ?? "Anonymous");

        try
        {
            await _next(context);
  }
        finally
        {
    stopwatch.Stop();

       // Log response
            var level = context.Response.StatusCode >= 500 
    ? LogLevel.Error 
       : context.Response.StatusCode >= 400 
     ? LogLevel.Warning 
      : LogLevel.Information;

         _logger.Log(level,
          "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms. RequestId: {RequestId}",
        context.Request.Method,
            context.Request.Path,
                context.Response.StatusCode,
              stopwatch.ElapsedMilliseconds,
              requestId);
        }
    }
}

/// <summary>
/// Extension method to register the request logging middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
