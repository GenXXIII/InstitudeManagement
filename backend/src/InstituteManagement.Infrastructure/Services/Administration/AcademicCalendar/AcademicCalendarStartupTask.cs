using InstituteManagement.Application.Common.Startup;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed class AcademicCalendarStartupTask(AcademicCalendarRolloverService calendar) : IApplicationStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken) =>
        await calendar.ApplyForCurrentDateAsync(cancellationToken);
}
