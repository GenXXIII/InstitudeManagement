using InstituteManagement.Application;
using InstituteManagement.Application.Common.Startup;
using InstituteManagement.Infrastructure;
using InstituteManagement.Infrastructure.Realtime;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<InstituteManagement.API.Middleware.ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
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

if (app.Environment.IsDevelopment())
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

public partial class Program;
