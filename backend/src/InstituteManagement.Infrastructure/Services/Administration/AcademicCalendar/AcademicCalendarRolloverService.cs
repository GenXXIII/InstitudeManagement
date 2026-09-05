using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed class AcademicCalendarRolloverService(
    InstituteDbContext db,
    InstituteCache cache,
    AcademicCalendarClock clock,
    StudentAcademicYearAdvancer studentAdvancer,
    ActivePeriodLedgerCreator ledgerCreator)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<bool> ApplyForCurrentDateAsync(CancellationToken cancellationToken) =>
        await ApplyAsync(await clock.GetTodayAsync(cancellationToken), cancellationToken);

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
            var hasSummerStart = TryDate(values, "semester:summerStartsOn", out var summerStart);
            var hasSummerEnd = TryDate(values, "semester:summerEndsOn", out var summerEnd);
            var hasSummer = hasSummerStart
                && hasSummerEnd
                && summerStart > semester2End
                && summerEnd >= summerStart;
            var yearRolloverEnd = hasSummer ? summerEnd : academicEnd;

            var changed = false;
            var promoted = 0;
            var graduated = 0;
            var yearsAdvanced = 0;
            while (today > yearRolloverEnd)
            {
                var oldYear = values.GetValueOrDefault("academic-year:currentYear", $"{academicStart.Year}\u2013{academicEnd.Year}");
                var advance = await studentAdvancer.AdvanceAsync(oldYear, cancellationToken);
                graduated += advance.Graduated;
                promoted += advance.Promoted;
                yearsAdvanced++;

                academicStart = academicStart.AddYears(1);
                academicEnd = academicEnd.AddYears(1);
                semester1Start = semester1Start.AddYears(1);
                semester1End = semester1End.AddYears(1);
                semester2Start = semester2Start.AddYears(1);
                semester2End = semester2End.AddYears(1);
                if (hasSummer)
                {
                    summerStart = summerStart.AddYears(1);
                    summerEnd = summerEnd.AddYears(1);
                }
                yearRolloverEnd = hasSummer ? summerEnd : academicEnd;
                changed = true;
            }

            var activeTerm = today <= semester1End
                ? "Semester 1"
                : today <= semester2End || !hasSummer
                    ? "Semester 2"
                    : "Summer Term";
            var (activeStart, activeEnd) = activeTerm switch
            {
                "Semester 1" => (semester1Start, semester1End),
                "Semester 2" => (semester2Start, semester2End),
                _ => (summerStart, summerEnd)
            };
            changed |= Set(settings, "academic-year", "currentYear", $"{academicStart.Year}\u2013{academicEnd.Year}");
            changed |= Set(settings, "academic-year", "startsOn", academicStart.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "academic-year", "endsOn", academicEnd.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester1StartsOn", semester1Start.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester1EndsOn", semester1End.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester2StartsOn", semester2Start.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester2EndsOn", semester2End.ToString("yyyy-MM-dd"));
            if (hasSummer)
            {
                changed |= Set(settings, "semester", "summerStartsOn", summerStart.ToString("yyyy-MM-dd"));
                changed |= Set(settings, "semester", "summerEndsOn", summerEnd.ToString("yyyy-MM-dd"));
            }
            changed |= Set(settings, "semester", "semester1Status", TermStatus(today, semester1End, activeTerm == "Semester 1"));
            changed |= Set(settings, "semester", "semester2Status", TermStatus(today, semester2End, activeTerm == "Semester 2"));
            if (hasSummer) changed |= Set(settings, "semester", "summerStatus", TermStatus(today, summerEnd, activeTerm == "Summer Term"));
            changed |= Set(settings, "semester", "currentTerm", activeTerm);
            changed |= Set(settings, "semester", "startsOn", activeStart.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "endsOn", activeEnd.ToString("yyyy-MM-dd"));

            if (!changed) return false;
            var activeYear = $"{academicStart.Year}\u2013{academicEnd.Year}";
            var (attendanceCreated, gradesCreated) = await ledgerCreator.CreateAsync(activeYear, activeTerm, activeStart, cancellationToken);
            if (yearsAdvanced == 0)
            {
                db.AuditLogs.Add(new AuditLog { Type = "Academic calendar", Subject = activeTerm, Action = "Semester rollover", Details = $"Activated {activeTerm}. Previous grade and attendance rows remain in History; Management now uses a new active-period ledger." });
            }
            db.Notifications.Add(new Notification
            {
                Title = yearsAdvanced > 0 ? "Academic year advanced" : $"{activeTerm} activated",
                Message = yearsAdvanced > 0 ? $"Advanced {yearsAdvanced} academic year(s), promoted {promoted} students, graduated {graduated} Year 4 students, and created {attendanceCreated} attendance and {gradesCreated} grade rows." : $"{activeTerm} created {attendanceCreated} attendance and {gradesCreated} grade rows; the previous semester is available in Records.",
                Severity = "Info"
            });
            await db.SaveChangesAsync(cancellationToken);
            await cache.InvalidateDashboardAsync(cancellationToken);
            return true;
        }
        finally { Gate.Release(); }
    }

    private static bool TryDate(IReadOnlyDictionary<string, string> values, string key, out DateOnly date) => DateOnly.TryParse(values.GetValueOrDefault(key), out date);

    private static string TermStatus(DateOnly today, DateOnly endsOn, bool active) => active ? "Active" : today > endsOn ? "Completed" : "Upcoming";

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
