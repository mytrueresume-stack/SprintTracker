using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SprintTracker.Api.Data;
using SprintTracker.Api.Models;
using SprintTracker.Api.Models.DTOs;
using BCrypt.Net;

namespace SprintTracker.Api.Services;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<UserDto?> GetCurrentUserAsync(string userId);
 Task<bool> UpdatePasswordAsync(string userId, string currentPassword, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly MongoDbContext _context;
    private readonly IConfiguration _configuration;
 private readonly ILogger<AuthService> _logger;

    public AuthService(MongoDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
    // Check if email already exists
    var existingUser = await _context.Users
       .Find(u => u.Email.ToLower() == request.Email.ToLower())
     .FirstOrDefaultAsync();

        if (existingUser != null)
        {
         _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
    return null;
    }

   // Map role - ensure it's a valid role (Admin=0, Manager=1, Developer=2)
         var role = request.Role;
            if ((int)role < 0 || (int)role > 2)
{
                role = UserRole.Developer; // Default to Developer for invalid roles
       }

     var user = new User
  {
 Email = request.Email.ToLower(),
         PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
           FirstName = request.FirstName.Trim(),
         LastName = request.LastName.Trim(),
      Role = role,
       CreatedAt = DateTime.UtcNow,
             UpdatedAt = DateTime.UtcNow
         };

  await _context.Users.InsertOneAsync(user);
    _logger.LogInformation("User registered successfully: {Email} with role {Role}", user.Email, user.Role);

   var token = GenerateJwtToken(user);
     return new AuthResponse(
                token,
   DateTime.UtcNow.AddMinutes(GetTokenExpiration()),
   MapToUserDto(user)
            );
  }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Error registering user");
     throw;
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
      try
        {
var user = await _context.Users
    .Find(u => u.Email.ToLower() == request.Email.ToLower())
      .FirstOrDefaultAsync();

     if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
 {
     _logger.LogWarning("Login failed for email: {Email}", request.Email);
  return null;
   }

     if (!user.IsActive)
      {
            _logger.LogWarning("Login attempt for inactive user: {Email}", request.Email);
    return null;
            }

        // Update last login
      var update = Builders<User>.Update
     .Set(u => u.LastLoginAt, DateTime.UtcNow);
      await _context.Users.UpdateOneAsync(u => u.Id == user.Id, update);

            var token = GenerateJwtToken(user);
       _logger.LogInformation("User logged in successfully: {Email}", user.Email);

   return new AuthResponse(
  token,
   DateTime.UtcNow.AddMinutes(GetTokenExpiration()),
      MapToUserDto(user)
          );
        }
   catch (Exception ex)
        {
    _logger.LogError(ex, "Error during login");
  throw;
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync(string userId)
    {
 var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        return user != null ? MapToUserDto(user) : null;
    }

  public async Task<bool> UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
    {
    var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
   if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        var update = Builders<User>.Update
   .Set(u => u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(newPassword))
         .Set(u => u.UpdatedAt, DateTime.UtcNow);

await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
 return true;
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
  {
  new Claim(JwtRegisteredClaimNames.Sub, user.Id),
   new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
      new Claim(ClaimTypes.Role, user.Role.ToString()),
       new Claim("role_id", ((int)user.Role).ToString()),
     new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

      var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
      audience: _configuration["JwtSettings:Audience"],
   claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetTokenExpiration()),
     signingCredentials: credentials
   );

 return new JwtSecurityTokenHandler().WriteToken(token);
    }

 private int GetTokenExpiration() =>
        int.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "60");

    private static UserDto MapToUserDto(User user) => new(
        user.Id,
        user.Email,
     user.FirstName,
 user.LastName,
        user.FullName,
        user.Role,
        user.Avatar,
        user.IsActive
  );
}
