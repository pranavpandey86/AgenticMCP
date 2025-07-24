using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgenticOrderingSystem.API.Models;

namespace AgenticOrderingSystem.API.Services;

public interface IAuthenticationService
{
    Task<LoginResponse> AuthenticateAsync(LoginRequest request, string ipAddress, string userAgent);
    Task<bool> ValidateTokenAsync(string token);
    Task<string?> GetUserIdFromTokenAsync(string token);
    Task<UserSession?> GetSessionAsync(string token);
    Task LogoutAsync(string token);
    Task<bool> IsSessionValidAsync(string token);
}

public interface IAuditService
{
    Task LogAsync(string? userId, string action, string resource, string? resourceId, string result, string ipAddress, string userAgent, Dictionary<string, object>? details = null);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IDatabaseService _databaseService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecret;

    public AuthenticationService(
        IDatabaseService databaseService,
        IUserService userService,
        ILogger<AuthenticationService> logger,
        IConfiguration configuration)
    {
        _databaseService = databaseService;
        _userService = userService;
        _logger = logger;
        _configuration = configuration;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "your-super-secret-jwt-key-change-in-production";
    }

    public async Task<LoginResponse> AuthenticateAsync(LoginRequest request, string ipAddress, string userAgent)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return new LoginResponse { Success = false, Error = "Invalid credentials" };
            }

            var token = GenerateJwtToken(user);
            
            var session = new UserSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                SessionToken = token,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _databaseService.UserSessions.InsertOneAsync(session);

            return new LoginResponse
            {
                Success = true,
                Token = token,
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                ExpiresAt = session.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for email: {Email}", request.Email);
            return new LoginResponse { Success = false, Error = "Authentication failed" };
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var session = await GetSessionAsync(token);
            return session?.IsActive == true && session.ExpiresAt > DateTime.UtcNow;
        }
        catch
        {
            return false;
        }
    }

    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "userId");
            return userIdClaim?.Value;
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserSession?> GetSessionAsync(string token)
    {
        var filter = Builders<UserSession>.Filter.Eq(s => s.SessionToken, token);
        return await _databaseService.UserSessions.Find(filter).FirstOrDefaultAsync();
    }

    public async Task LogoutAsync(string token)
    {
        var filter = Builders<UserSession>.Filter.Eq(s => s.SessionToken, token);
        var update = Builders<UserSession>.Update.Set(s => s.IsActive, false);
        await _databaseService.UserSessions.UpdateOneAsync(filter, update);
    }

    public async Task<bool> IsSessionValidAsync(string token)
    {
        var session = await GetSessionAsync(token);
        if (session == null || !session.IsActive || session.ExpiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        var filter = Builders<UserSession>.Filter.Eq(s => s.Id, session.Id);
        var update = Builders<UserSession>.Update.Set(s => s.LastAccessedAt, DateTime.UtcNow);
        await _databaseService.UserSessions.UpdateOneAsync(filter, update);

        return true;
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("userId", user.Id),
                new Claim("email", user.Email),
                new Claim("fullName", $"{user.FirstName} {user.LastName}")
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class AuditService : IAuditService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IDatabaseService databaseService, ILogger<AuditService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task LogAsync(string? userId, string action, string resource, string? resourceId, string result, string ipAddress, string userAgent, Dictionary<string, object>? details = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Resource = resource,
                ResourceId = resourceId,
                Result = result,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Details = details ?? new Dictionary<string, object>()
            };

            await _databaseService.AuditLogs.InsertOneAsync(auditLog);
            _logger.LogInformation("Audit log created: {Action} on {Resource} by {UserId} - {Result}", action, resource, userId, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log");
        }
    }
}
