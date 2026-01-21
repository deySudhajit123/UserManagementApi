namespace UserManagementApi.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task Invoke(HttpContext context)
    {
        // Allow swagger without auth (optional, but convenient)
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        var expectedKey = _config["ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Server ApiKey is not configured.");
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var providedKey) ||
            providedKey != expectedKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing or invalid API key.");
            return;
        }

        await _next(context);
    }
}
