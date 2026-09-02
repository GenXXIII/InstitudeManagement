using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.API.Middleware;

public sealed class MaintenanceModeMiddleware(RequestDelegate next, ILogger<MaintenanceModeMiddleware> logger)
{
    private const string DefaultMessage = "System is currently under maintenance. Please try again later.";

    public async Task InvokeAsync(HttpContext context, InstituteDbContext db)
    {
        if (IsAlwaysAvailable(context.Request))
        {
            await next(context);
            return;
        }

        Dictionary<string, string> settings;
        try
        {
            settings = await db.SystemSettings.AsNoTracking()
                .Where(setting => setting.Section == "system" &&
                    (setting.Key == "maintenanceEnabled" || setting.Key == "maintenanceMessage"))
                .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, StringComparer.OrdinalIgnoreCase, context.RequestAborted);
        }
        catch (Exception reason)
        {
            logger.LogWarning(reason, "Maintenance state could not be read; the request will continue.");
            await next(context);
            return;
        }

        if (!settings.TryGetValue("maintenanceEnabled", out var enabledValue) ||
            !bool.TryParse(enabledValue, out var enabled) || !enabled)
        {
            await next(context);
            return;
        }

        var message = settings.GetValueOrDefault("maintenanceMessage");
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers["Retry-After"] = "300";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/503",
            title = "Institude of New Khmer is under maintenance",
            status = StatusCodes.Status503ServiceUnavailable,
            detail = string.IsNullOrWhiteSpace(message) ? DefaultMessage : message,
        }, context.RequestAborted);
    }

    private static bool IsAlwaysAvailable(HttpRequest request) =>
        HttpMethods.IsOptions(request.Method) ||
        request.Path.StartsWithSegments("/health") ||
        request.Path.StartsWithSegments("/api/settings") ||
        request.Path.StartsWithSegments("/uploads");
}
