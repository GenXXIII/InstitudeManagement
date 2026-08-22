using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed class SettingsService : ISettingsService
{
    private readonly InstituteDbContext db;
    private readonly InstituteCache cache;
    private readonly AcademicCalendarRolloverService? calendar;
    private static readonly HashSet<string> Sections = ["institute", "academic-year", "semester", "departments", "courses", "classrooms", "attendance-rules", "grade-rules", "notifications", "system"];

    public SettingsService(InstituteDbContext db, InstituteCache cache, AcademicCalendarRolloverService? calendar = null)
    {
        this.db = db;
        this.cache = cache;
        this.calendar = calendar;
    }

    public async Task<SettingsDto> GetAsync(string section, CancellationToken ct)
    {
        EnsureSection(section);
        return new SettingsDto(section, await db.SystemSettings.AsNoTracking().Where(x => x.Section == section && !(section == "courses" && x.Key == "defaultCredits")).ToDictionaryAsync(x => x.Key, x => x.Value, ct));
    }

    public async Task<SettingsDto> SaveAsync(string section, Dictionary<string, string> values, CancellationToken ct)
    {
        EnsureSection(section);
        foreach (var item in values)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > 128) throw new ArgumentException("Setting names must contain 1 to 128 characters.");
            if (item.Value is null) throw new ArgumentException($"{item.Key} requires a value.");
            if (item.Value.Length > 2048) throw new ArgumentException($"{item.Key} must contain no more than 2048 characters.");
        }

        var existing = await db.SystemSettings.Where(x => x.Section == section).ToListAsync(ct);
        var normalized = values.ToDictionary(item => item.Key, item => item.Value.Trim());
        Validate(section, normalized);

        db.SystemSettings.RemoveRange(existing.Where(setting => !values.ContainsKey(setting.Key)));

        foreach (var item in normalized)
        {
            var setting = existing.FirstOrDefault(x => x.Key == item.Key);
            if (setting is null) db.SystemSettings.Add(new SystemSetting { Section = section, Key = item.Key, Value = item.Value });
            else { setting.Value = item.Value; setting.UpdatedAtUtc = DateTime.UtcNow; }
        }

        if (section == "grade-rules")
        {
            var scale = GradeThresholds.From(normalized);
            foreach (var grade in await db.GradeRecords.ToListAsync(ct)) { grade.LetterGrade = scale.Letter(grade.Score); grade.UpdatedAtUtc = DateTime.UtcNow; }
        }

        db.AuditLogs.Add(new AuditLog { Type = "Settings", Subject = section, Action = "Updated", Details = $"Changed configuration: {string.Join(", ", values.Keys.Order())}" });
        db.Notifications.Add(new Notification { Title = "Configuration updated", Message = $"{section} settings now apply across the institute.", Severity = "Info" });
        await db.SaveChangesAsync(ct);
        await cache.InvalidateDashboardAsync(ct);
        if (calendar is not null && (section is "academic-year" or "semester")) await calendar.ApplyForCurrentDateAsync(ct);
        var applied = await db.SystemSettings.AsNoTracking().Where(x => x.Section == section && !(section == "courses" && x.Key == "defaultCredits")).ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        return new SettingsDto(section, applied);
    }

    private static void EnsureSection(string section) { if (!Sections.Contains(section)) throw new KeyNotFoundException("Settings section not found."); }

    private static void Validate(string section, IReadOnlyDictionary<string, string> values)
    {
        string Required(string key) => !string.IsNullOrWhiteSpace(values.GetValueOrDefault(key)) ? values[key] : throw new ArgumentException($"{key} is required.");
        int Integer(string key, int minimum, int maximum) => int.TryParse(values.GetValueOrDefault(key), out var value) && value >= minimum && value <= maximum ? value : throw new ArgumentException($"{key} must be between {minimum} and {maximum}.");
        bool Boolean(string key) => bool.TryParse(values.GetValueOrDefault(key), out var value) ? value : throw new ArgumentException($"{key} must be true or false.");
        void DateWindow() { var start = DateOnly.TryParse(Required("startsOn"), out var startDate) ? startDate : throw new ArgumentException("startsOn must be a date."); var end = DateOnly.TryParse(Required("endsOn"), out var endDate) ? endDate : throw new ArgumentException("endsOn must be a date."); if (end <= start) throw new ArgumentException("End date must be after start date."); }

        if (section == "institute") { Required("name"); Required("shortName"); Required("email"); Required("phone"); Required("address"); }
        else if (section == "academic-year") { Required("currentYear"); DateWindow(); }
        else if (section == "semester")
        {
            if (Required("currentTerm") is not ("Semester 1" or "Semester 2")) throw new ArgumentException("currentTerm must be Semester 1 or Semester 2.");
            DateWindow();
            var semester1Start = DateOnly.TryParse(Required("semester1StartsOn"), out var firstStart) ? firstStart : throw new ArgumentException("semester1StartsOn must be a date.");
            var semester1End = DateOnly.TryParse(Required("semester1EndsOn"), out var firstEnd) ? firstEnd : throw new ArgumentException("semester1EndsOn must be a date.");
            var semester2Start = DateOnly.TryParse(Required("semester2StartsOn"), out var secondStart) ? secondStart : throw new ArgumentException("semester2StartsOn must be a date.");
            var semester2End = DateOnly.TryParse(Required("semester2EndsOn"), out var secondEnd) ? secondEnd : throw new ArgumentException("semester2EndsOn must be a date.");
            if (!(semester1Start < semester1End && semester1End < semester2Start && semester2Start < semester2End)) throw new ArgumentException("Semester dates must be ordered from Semester 1 start through Semester 2 end.");
        }
        else if (section == "departments") { Boolean("requireDepartmentHead"); Boolean("allowCrossDepartmentTeaching"); if (Required("defaultStatus") is not ("Active" or "Inactive")) throw new ArgumentException("defaultStatus must be Active or Inactive."); }
        else if (section == "courses") { Integer("defaultCapacity", 1, 10000); Boolean("requireAssignedTeacher"); }
        else if (section == "classrooms") { Integer("defaultCapacity", 1, 10000); Boolean("attendanceDeviceRequired"); Boolean("allowSharedRooms"); }
        else if (section == "attendance-rules") { if (Required("method") is not ("Manual" or "ID Card" or "QR Code" or "Biometric")) throw new ArgumentException("Attendance method is invalid."); Integer("lateThresholdMinutes", 0, 1440); foreach (var key in new[] { "autoAbsent", "autoPercentage", "notifyTeacher", "notifyAdministrator", "allowCorrection", "requireCorrectionReason" }) Boolean(key); }
        else if (section == "grade-rules") { var scale = GradeThresholds.From(values); if (!(scale.A <= 100 && scale.A > scale.B && scale.B > scale.C && scale.C > scale.D && scale.D > scale.E && scale.E >= 0)) throw new ArgumentException("Grade thresholds must descend from A through E; scores below E are F."); }
        else if (section == "notifications") foreach (var key in new[] { "attendanceAlerts", "deviceAlerts", "gradeReminders", "dailySummary" }) Boolean(key);
        else if (section == "system") { Required("language"); Required("dateFormat"); Integer("autoRefreshSeconds", 5, 3600); try { TimeZoneInfo.FindSystemTimeZoneById(Required("timeZone")); } catch (TimeZoneNotFoundException) { throw new ArgumentException("timeZone is invalid."); } }
    }
}
