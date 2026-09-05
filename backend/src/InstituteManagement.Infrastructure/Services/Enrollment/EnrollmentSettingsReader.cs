using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Enrollment;

internal sealed class EnrollmentSettingsReader
{
    private readonly InstituteDbContext db;

    public EnrollmentSettingsReader(InstituteDbContext db)
    {
        this.db = db;
    }

    public async Task<EnrollmentPeriod> CurrentPeriodAsync(CancellationToken cancellationToken)
    {
        var values = await db.SystemSettings
            .AsNoTracking()
            .Where(setting =>
                (setting.Section == "academic-year" && setting.Key == "currentYear")
                || (setting.Section == "semester" && setting.Key == "currentTerm"))
            .ToDictionaryAsync(
                setting => $"{setting.Section}:{setting.Key}",
                setting => setting.Value,
                cancellationToken);

        return new EnrollmentPeriod(
            values.GetValueOrDefault("academic-year:currentYear", "2026–2027"),
            values.GetValueOrDefault("semester:currentTerm", "Semester 1"));
    }

    public async Task<bool> EnabledAsync(
        string section,
        string key,
        bool fallback,
        CancellationToken cancellationToken)
    {
        var value = await db.SystemSettings
            .AsNoTracking()
            .Where(setting => setting.Section == section && setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return bool.TryParse(value, out var enabled) ? enabled : fallback;
    }

    public async Task<int> IntegerAsync(
        string section,
        string key,
        int fallback,
        int minimum,
        int maximum,
        CancellationToken cancellationToken)
    {
        var value = await db.SystemSettings
            .AsNoTracking()
            .Where(setting => setting.Section == section && setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return int.TryParse(value, out var configured)
            && configured >= minimum
            && configured <= maximum
                ? configured
                : fallback;
    }
}

internal sealed record EnrollmentPeriod(string AcademicYear, string Semester);
