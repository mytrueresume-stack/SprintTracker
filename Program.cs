using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using SprintTracker.Api.Data;
using SprintTracker.Api.Filters;
using SprintTracker.Api.Middleware;
using SprintTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add MongoDB Context
builder.Services.AddSingleton<MongoDbContext>();

// Add Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ISprintService, SprintService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISprintSubmissionService, SprintSubmissionService>();

// Add Controllers with JSON options
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
    // Add normalization filter before validation so DTOs are normalized first
    options.Filters.Add<ModelNormalizationFilter>();
    // Add global model validation filter
    options.Filters.Add<ModelValidationFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Prevent automatic400 on model validation so controllers can log and return standardized ApiResponse
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Add Problem Details
builder.Services.AddProblemDetails();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.FromMinutes(1) // Reduce default 5-minute clock skew
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                logger.LogDebug("Token validated for user: {UserId}", userId);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Global rate limit - increased for development
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
    AutoReplenishment = true,
        PermitLimit = 500, // Increased from 100
     Window = TimeSpan.FromMinutes(1)
            });
    });

    // Specific policy for authentication endpoints (stricter)
    options.AddFixedWindowLimiter("auth", opt =>
  {
        opt.PermitLimit = 20; // Increased from 10
        opt.Window = TimeSpan.FromMinutes(1);
     opt.AutoReplenishment = true;
  });

    // Policy for API endpoints
    options.AddFixedWindowLimiter("api", opt =>
    {
      opt.PermitLimit = 200; // Increased from 60
        opt.Window = TimeSpan.FromMinutes(1);
        opt.AutoReplenishment = true;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
      logger.LogWarning("Rate limit exceeded for {User} on {Path}",
     context.HttpContext.User?.Identity?.Name ?? "Anonymous",
   context.HttpContext.Request.Path);

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
  context.HttpContext.Response.ContentType = "application/json";
        
     var response = new { success = false, message = "Too many requests. Please try again later." };
        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };
});

// Configure CORS for Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJS", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
          ?? new[] { "http://localhost:3000", "http://127.0.0.1:3000" };
        
 policy.WithOrigins(allowedOrigins)
  .AllowAnyHeader()
.AllowAnyMethod()
        .AllowCredentials()
     .WithExposedHeaders("X-Pagination", "X-Request-Id");
    });
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
{
     Title = "SprintTracker API - Mphasis",
      Version = "v1",
        Description = "Sprint Tracking and Management System for Mphasis",
    Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
    Name = "SprintTracker Support",
        Email = "support@sprinttracker.com"
      }
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
      Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
      Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

 c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
{
{
       new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
     Reference = new Microsoft.OpenApi.Models.OpenApiReference
          {
  Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
           Id = "Bearer"
    }
   },
 Array.Empty<string>()
   }
    });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck<MongoDbHealthCheck>("mongodb");

// Add HttpContext accessor for logging
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
// Order matters! Exception handler should be first
app.UseGlobalExceptionHandler();

// Request logging for observability
app.UseRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
     c.SwaggerEndpoint("/swagger/v1/swagger.json", "SprintTracker API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowNextJS");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
      {
         name = e.Key,
          status = e.Value.Status.ToString(),
  description = e.Value.Description,
         duration = e.Value.Duration.TotalMilliseconds
          })
        };
     await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Always returns healthy if app is running
});

app.Run();

/// <summary>
/// MongoDB health check implementation
/// </summary>
public class MongoDbHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly MongoDbContext _context;
  private readonly ILogger<MongoDbHealthCheck> _logger;

    public MongoDbHealthCheck(MongoDbContext context, ILogger<MongoDbHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
   // Try to ping the database by counting documents (simpler approach)
     await _context.Users.CountDocumentsAsync(
       MongoDB.Driver.Builders<SprintTracker.Api.Models.User>.Filter.Empty, 
   new MongoDB.Driver.CountOptions { MaxTime = TimeSpan.FromSeconds(5) },
          cancellationToken);
         return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("MongoDB connection is healthy");
        }
        catch (Exception ex)
        {
      _logger.LogError(ex, "MongoDB health check failed");
    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
        "MongoDB connection is unhealthy", ex);
      }
    }
}
