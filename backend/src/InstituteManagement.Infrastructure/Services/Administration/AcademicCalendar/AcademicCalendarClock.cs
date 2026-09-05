using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed class AcademicCalendarClock(InstituteDbContext db)
{
    public async Task<DateOnly> GetTodayAsync(CancellationToken cancellationToken)
    {
        var timeZoneId = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "system" && setting.Key == "timeZone")
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken) ?? "Asia/Bangkok";

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
    }
}
