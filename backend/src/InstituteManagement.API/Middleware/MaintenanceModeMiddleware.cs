using InstituteManagement.Application.Features.Administration.Maintenance;

namespace InstituteManagement.API.Middleware;

public sealed class MaintenanceModeMiddleware(RequestDelegate next, ILogger<MaintenanceModeMiddleware> logger)
{
    private const string DefaultMessage = "System is currently under maintenance. Please try again later.";

    public async Task InvokeAsync(HttpContext context, IMaintenanceModeReader maintenanceMode)
    {
        if (IsAlwaysAvailable(context.Request))
        {
            await next(context);
            return;
        }

        MaintenanceModeState state;
        try
        {
            state = await maintenanceMode.GetAsync(context.RequestAborted);
        }
        catch (Exception reason)
        {
            logger.LogWarning(reason, "Maintenance state could not be read; the request will continue.");
            await next(context);
            return;
        }

        if (!state.Enabled)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers["Retry-After"] = "300";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/503",
            title = "Institude of New Khmer is under maintenance",
            status = StatusCodes.Status503ServiceUnavailable,
            detail = string.IsNullOrWhiteSpace(state.Message) ? DefaultMessage : state.Message,
        }, context.RequestAborted);
    }

    private static bool IsAlwaysAvailable(HttpRequest request) =>
        HttpMethods.IsOptions(request.Method) ||
        request.Path.StartsWithSegments("/health") ||
        request.Path.StartsWithSegments("/api/settings") ||
        request.Path.StartsWithSegments("/uploads");
}
