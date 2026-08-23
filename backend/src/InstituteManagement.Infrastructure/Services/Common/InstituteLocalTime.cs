using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Common;

internal static class InstituteLocalTime
{
    public static async Task<DateTime> NowAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var timeZoneId = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "system" && setting.Key == "timeZone")
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken) ?? "Asia/Bangkok";
        try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)); }
        catch (TimeZoneNotFoundException) { return DateTime.UtcNow; }
        catch (InvalidTimeZoneException) { return DateTime.UtcNow; }
    }
}
