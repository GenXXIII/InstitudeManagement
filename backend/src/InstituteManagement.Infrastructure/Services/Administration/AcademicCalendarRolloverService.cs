using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed class AcademicCalendarRolloverService(InstituteDbContext db, InstituteCache cache)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<bool> ApplyForCurrentDateAsync(CancellationToken cancellationToken)
    {
        var timeZoneId = await db.SystemSettings.AsNoTracking()
            .Where(x => x.Section == "system" && x.Key == "timeZone")
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken) ?? "Asia/Bangkok";
        TimeZoneInfo timeZone;
        try { timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { timeZone = TimeZoneInfo.Utc; }
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        return await ApplyAsync(today, cancellationToken);
    }

    public async Task<bool> ApplyAsync(DateOnly today, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var settings = await db.SystemSettings
                .Where(x => x.Section == "academic-year" || x.Section == "semester")
                .ToListAsync(cancellationToken);
            var values = settings.ToDictionary(x => $"{x.Section}:{x.Key}", x => x.Value);
            if (!TryDate(values, "academic-year:startsOn", out var academicStart)
                || !TryDate(values, "academic-year:endsOn", out var academicEnd)
                || !TryDate(values, "semester:semester1StartsOn", out var semester1Start)
                || !TryDate(values, "semester:semester1EndsOn", out var semester1End)
                || !TryDate(values, "semester:semester2StartsOn", out var semester2Start)
                || !TryDate(values, "semester:semester2EndsOn", out var semester2End)) return false;

            var changed = false;
            var promoted = 0;
            var yearsAdvanced = 0;
            while (today > semester2End)
            {
                var oldYear = values.GetValueOrDefault("academic-year:currentYear", $"{academicStart.Year}\u2013{academicEnd.Year}");
                var students = await db.Students.Where(x => x.Status != "Inactive" && x.YearLevel >= 1 && x.YearLevel < 4).ToListAsync(cancellationToken);
                foreach (var student in students) { student.YearLevel++; student.UpdatedAtUtc = DateTime.UtcNow; }
                promoted += students.Count;
                yearsAdvanced++;

                academicStart = academicStart.AddYears(1);
                academicEnd = academicEnd.AddYears(1);
                semester1Start = semester1Start.AddYears(1);
                semester1End = semester1End.AddYears(1);
                semester2Start = semester2Start.AddYears(1);
                semester2End = semester2End.AddYears(1);
                db.AuditLogs.Add(new AuditLog
                {
                    Type = "Academic calendar",
                    Subject = oldYear,
                    Action = "Year rollover",
                    Details = $"Closed {oldYear}; promoted {students.Count} active Year 1-3 students. Year 4 students were preserved. Grade and attendance rows remain in history."
                });
                changed = true;
            }

            var activeTerm = today > semester1End ? "Semester 2" : "Semester 1";
            var activeStart = activeTerm == "Semester 1" ? semester1Start : semester2Start;
            var activeEnd = activeTerm == "Semester 1" ? semester1End : semester2End;
            changed |= Set(settings, "academic-year", "currentYear", $"{academicStart.Year}\u2013{academicEnd.Year}");
            changed |= Set(settings, "academic-year", "startsOn", academicStart.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "academic-year", "endsOn", academicEnd.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester1StartsOn", semester1Start.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester1EndsOn", semester1End.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester2StartsOn", semester2Start.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester2EndsOn", semester2End.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "currentTerm", activeTerm);
            changed |= Set(settings, "semester", "startsOn", activeStart.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "endsOn", activeEnd.ToString("yyyy-MM-dd"));

            if (!changed) return false;
            if (yearsAdvanced == 0)
            {
                db.AuditLogs.Add(new AuditLog { Type = "Academic calendar", Subject = activeTerm, Action = "Semester rollover", Details = $"Activated {activeTerm}. Previous grade and attendance rows remain in Records history; Management now uses a new active-period ledger." });
            }
            db.Notifications.Add(new Notification
            {
                Title = yearsAdvanced > 0 ? "Academic year advanced" : $"{activeTerm} activated",
                Message = yearsAdvanced > 0 ? $"Advanced {yearsAdvanced} academic year(s) and promoted {promoted} active students. Current ledgers are ready." : $"Grade and attendance Management now use {activeTerm}; the previous semester is available in Records.",
                Severity = "Info"
            });
            await db.SaveChangesAsync(cancellationToken);
            await cache.InvalidateDashboardAsync(cancellationToken);
            return true;
        }
        finally { Gate.Release(); }
    }

    private static bool TryDate(IReadOnlyDictionary<string, string> values, string key, out DateOnly date) => DateOnly.TryParse(values.GetValueOrDefault(key), out date);

    private bool Set(List<SystemSetting> settings, string section, string key, string value)
    {
        var setting = settings.FirstOrDefault(x => x.Section == section && x.Key == key);
        if (setting is null)
        {
            setting = new SystemSetting { Section = section, Key = key, Value = value };
            settings.Add(setting);
            db.SystemSettings.Add(setting);
            return true;
        }
        if (setting.Value == value) return false;
        setting.Value = value;
        setting.UpdatedAtUtc = DateTime.UtcNow;
        return true;
    }
}
