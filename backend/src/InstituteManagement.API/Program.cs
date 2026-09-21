using InstituteManagement.Application;
using InstituteManagement.Application.Common.Startup;
using InstituteManagement.Infrastructure;
using InstituteManagement.Infrastructure.Realtime;
using Microsoft.Extensions.FileProviders;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<InstituteManagement.API.Middleware.ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
var configuredCorsOrigins = (builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .SetIsOriginAllowed(origin => IsAllowedOrigin(origin, configuredCorsOrigins))
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
});
app.UseMiddleware<InstituteManagement.API.Middleware.MaintenanceModeMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.MapHub<InstituteHub>("/hubs/institute");
app.MapHealthChecks("/health");

await app.Services.InitializeInfrastructureAsync();
using (var scope = app.Services.CreateScope())
{
    foreach (var startupTask in scope.ServiceProvider.GetServices<IApplicationStartupTask>())
        await startupTask.ExecuteAsync(CancellationToken.None);
}

app.Run();

static bool IsAllowedOrigin(string origin, IReadOnlySet<string> configuredOrigins)
{
    if (configuredOrigins.Contains(origin)) return true;
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp || uri.Port is not (8081 or 8082)) return false;
    if (uri.IsLoopback) return true;
    if (!IPAddress.TryParse(uri.Host, out var address)) return false;
    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;

    var bytes = address.GetAddressBytes();
    return bytes[0] == 10
        || bytes[0] == 192 && bytes[1] == 168
        || bytes[0] == 172 && bytes[1] is >= 16 and <= 31;
}

public partial class Program;
