using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SprintTracker.Api.Models.DTOs;

namespace SprintTracker.Api.Filters;

public class ModelValidationFilter : IActionFilter
{
 public void OnActionExecuting(ActionExecutingContext context)
 {
 if (!context.ModelState.IsValid)
 {
 var errors = context.ModelState.Values
 .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
 .ToList();

 // Log validation errors and action arguments for debugging
 try
 {
 var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ModelValidationFilter>>();
 var argsJson = System.Text.Json.JsonSerializer.Serialize(context.ActionArguments);
 logger.LogWarning("Model validation failed for {Path}: {Errors}. ActionArgs: {Args}", context.HttpContext.Request.Path, string.Join(" | ", errors), argsJson);
 }
 catch { /* swallow logging errors to avoid hiding the original validation problem */ }

 var response = new ApiResponse<object>(false, null, "Validation failed", errors);

 context.Result = new JsonResult(response)
 {
 StatusCode = StatusCodes.Status400BadRequest,
 ContentType = "application/json"
 };
 }
 }

 public void OnActionExecuted(ActionExecutedContext context)
 {
 // no-op
 }
}
