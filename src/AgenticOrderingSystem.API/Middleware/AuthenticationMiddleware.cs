using AgenticOrderingSystem.API.Services;

namespace AgenticOrderingSystem.API.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthenticationService authService, IAuditService auditService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        if (IsPublicEndpoint(path))
        {
            await _next(context);
            return;
        }

        var token = ExtractToken(context);
        if (string.IsNullOrEmpty(token))
        {
            await auditService.LogAsync(null, "ACCESS_DENIED", "API", path, "unauthorized", 
                GetIpAddress(context), GetUserAgent(context));
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Authentication required");
            return;
        }

        if (!await authService.ValidateTokenAsync(token))
        {
            await auditService.LogAsync(null, "ACCESS_DENIED", "API", path, "invalid_token", 
                GetIpAddress(context), GetUserAgent(context));
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid or expired token");
            return;
        }

        var userId = authService.GetUserIdFromToken(token);
        if (string.IsNullOrEmpty(userId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid token");
            return;
        }

        context.Items["UserId"] = userId;
        context.Items["Token"] = token;

        await _next(context);
    }

    private bool IsPublicEndpoint(string path)
    {
        var publicPaths = new[]
        {
            "/api/auth/login",
            "/api/dev/health",
            "/swagger",
            "/api/dev/seed"
        };

        return publicPaths.Any(p => path.StartsWith(p));
    }

    private string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        return null;
    }

    private string GetIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserAgent(HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
    }
}
