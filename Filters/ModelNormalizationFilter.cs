using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace SprintTracker.Api.Filters;

public class ModelNormalizationFilter : IActionFilter
{
 public void OnActionExecuting(ActionExecutingContext context)
 {
 foreach (var arg in context.ActionArguments)
 {
 if (arg.Value == null) continue;

 NormalizeObject(arg.Value);
 }
 }

 public void OnActionExecuted(ActionExecutedContext context)
 {
 // no-op
 }

 private void NormalizeObject(object obj)
 {
 var type = obj.GetType();

 // Only handle class/record types
 if (type.IsPrimitive || type == typeof(string)) return;

 var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
 .Where(p => p.CanRead && p.CanWrite);

 foreach (var p in props)
 {
 try
 {
 var propType = p.PropertyType;
 var val = p.GetValue(obj);

 if (propType == typeof(string))
 {
 var s = val as string;
 if (s != null)
 {
 s = s.Trim();
 // If property name is Key, normalize to uppercase
 if (string.Equals(p.Name, "Key", StringComparison.OrdinalIgnoreCase))
 {
 s = s.ToUpperInvariant();
 }

 // Convert empty strings to null to avoid binder issues
 if (s.Length ==0)
 {
 p.SetValue(obj, null);
 }
 else
 {
 p.SetValue(obj, s);
 }
 }
 }
 else if (!propType.IsPrimitive && !propType.IsEnum && !propType.IsValueType)
 {
 // Complex object - recurse
 if (val != null)
 NormalizeObject(val);
 }
 }
 catch
 {
 // swallow any exception to avoid breaking request processing
 }
 }
 }
}
