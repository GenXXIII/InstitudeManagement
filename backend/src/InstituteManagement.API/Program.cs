using InstituteManagement.API.Hubs;
using InstituteManagement.API.Services;
using InstituteManagement.Application;
using InstituteManagement.Application.Abstractions;
using InstituteManagement.Infrastructure;
using InstituteManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ILiveUpdatePublisher, SignalRLiveUpdatePublisher>();
builder.Services.AddControllers();
builder.Services.AddSignalR();
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<InstituteHub>("/hubs/institute");
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider.GetRequiredService<InstituteDbContext>());
    await scope.ServiceProvider.GetRequiredService<InstituteManagement.Infrastructure.Services.Record.ClassSessionRecorderService>().RecordCompletedForCurrentTimeAsync(CancellationToken.None);
    await scope.ServiceProvider.GetRequiredService<InstituteManagement.Infrastructure.Services.Administration.AcademicCalendarRolloverService>().ApplyForCurrentDateAsync(CancellationToken.None);
}

app.Run();

public partial class Program;
