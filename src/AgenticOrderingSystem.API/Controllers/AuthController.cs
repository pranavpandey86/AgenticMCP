using Microsoft.AspNetCore.Mvc;
using AgenticOrderingSystem.API.Services;
using AgenticOrderingSystem.API.Models;

namespace AgenticOrderingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authService,
        IAuditService auditService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";

            var response = await _authService.AuthenticateAsync(request, ipAddress, userAgent);

            await _auditService.LogAsync(
                response.Success ? response.UserId : null,
                "LOGIN",
                "AUTH",
                null,
                response.Success ? "success" : "failure",
                ipAddress,
                userAgent,
                new Dictionary<string, object> { ["email"] = request.Email }
            );

            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", request.Email);
            return StatusCode(500, new { error = "Login failed" });
        }
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var token = HttpContext.Items["Token"]?.ToString();
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (!string.IsNullOrEmpty(token))
            {
                await _authService.LogoutAsync(token);
            }

            await _auditService.LogAsync(
                userId,
                "LOGOUT",
                "AUTH",
                null,
                "success",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown"
            );

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return StatusCode(500, new { error = "Logout failed" });
        }
    }

    [HttpGet("validate")]
    public ActionResult ValidateToken()
    {
        var userId = HttpContext.Items["UserId"]?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        return Ok(new { valid = true, userId });
    }
}
