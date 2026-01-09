namespace SprintTracker.Api.Exceptions;

/// <summary>
/// Base exception for domain-specific errors
/// </summary>
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }

 protected DomainException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    protected DomainException(string message, string errorCode, Exception innerException) 
   : base(message, innerException)
    {
  ErrorCode = errorCode;
 }
}

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public class NotFoundException : DomainException
{
 public string ResourceType { get; }
    public string ResourceId { get; }

    public NotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} with ID '{resourceId}' was not found.", "RESOURCE_NOT_FOUND")
    {
  ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message, string errorCode = "BUSINESS_RULE_VIOLATION")
        : base(message, errorCode)
    {
    }
}

/// <summary>
/// Exception thrown when a user doesn't have permission to perform an action
/// </summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message, "FORBIDDEN")
    {
    }
}

/// <summary>
/// Exception thrown when input validation fails
/// </summary>
public class ValidationException : DomainException
{
 public Dictionary<string, List<string>> ValidationErrors { get; }

    public ValidationException(Dictionary<string, List<string>> validationErrors)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR")
    {
        ValidationErrors = validationErrors;
    }

    public ValidationException(string fieldName, string errorMessage)
        : base($"Validation error for field '{fieldName}': {errorMessage}", "VALIDATION_ERROR")
    {
        ValidationErrors = new Dictionary<string, List<string>>
  {
 { fieldName, new List<string> { errorMessage } }
        };
    }
}

/// <summary>
/// Exception thrown when a duplicate resource is detected
/// </summary>
public class DuplicateResourceException : DomainException
{
    public string ResourceType { get; }
    public string DuplicateValue { get; }

 public DuplicateResourceException(string resourceType, string duplicateValue)
  : base($"A {resourceType} with the value '{duplicateValue}' already exists.", "DUPLICATE_RESOURCE")
    {
        ResourceType = resourceType;
        DuplicateValue = duplicateValue;
    }
}

/// <summary>
/// Exception thrown when an operation conflicts with the current state
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message, "CONFLICT")
  {
 }
}

/// <summary>
/// Exception thrown when a service is temporarily unavailable
/// </summary>
public class ServiceUnavailableException : DomainException
{
    public ServiceUnavailableException(string serviceName)
      : base($"The {serviceName} service is temporarily unavailable. Please try again later.", "SERVICE_UNAVAILABLE")
    {
    }

    public ServiceUnavailableException(string serviceName, Exception innerException)
    : base($"The {serviceName} service is temporarily unavailable. Please try again later.", "SERVICE_UNAVAILABLE", innerException)
    {
    }
}
