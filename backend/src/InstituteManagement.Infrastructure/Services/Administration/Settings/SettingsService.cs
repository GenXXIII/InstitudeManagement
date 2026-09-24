using InstituteManagement.Application.Features.Administration;
using InstituteManagement.Application.Features.Administration.Settings;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Policies;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed class SettingsService(
    InstituteDbContext db,
    InstituteCache cache,
    AcademicCalendarRolloverService calendar) : ISettingsService
{
    private static readonly HashSet<string> GradeThresholdKeys =
    [
        "aMinimum", "bMinimum", "cMinimum", "dMinimum", "eMinimum"
    ];
    private static readonly HashSet<string> GradeWeightKeys =
    [
        "attendanceWeight", "assignmentWeight", "midtermWeight", "finalExamWeight"
    ];
    private static readonly HashSet<string> AttendanceResultKeys =
    [
        "absentScoreDeduction", "permissionScoreDeduction", "retakeAbsentSections", "failAbsentSections",
        "retakePermissionSections", "failPermissionSections"
    ];

    public async Task<IReadOnlyList<SettingsDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sectionNames = SettingsCatalog.Sections.Select(section => section.Name).ToArray();
        var stored = await db.SystemSettings.AsNoTracking()
            .Where(setting => sectionNames.Contains(setting.Section))
            .ToListAsync(cancellationToken);
        var bySection = stored
            .GroupBy(setting => setting.Section, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        return SettingsCatalog.Sections
            .Select(section => CreateDto(section, bySection.GetValueOrDefault(section.Name) ?? []))
            .ToList();
    }

    public async Task<SettingsDto> GetAsync(string section, CancellationToken cancellationToken)
    {
        var definition = SettingsCatalog.GetSection(section);
        var stored = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == definition.Name)
            .ToListAsync(cancellationToken);
        return CreateDto(definition, stored);
    }

    public async Task<SettingsDto> SaveAsync(string section, Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        var definition = SettingsCatalog.GetSection(section);
        var normalized = SettingsCatalog.NormalizeAndValidate(definition.Name, values);
        var existing = await db.SystemSettings
            .Where(setting => setting.Section == definition.Name)
            .ToListAsync(cancellationToken);
        var byKey = existing.ToDictionary(setting => setting.Key, StringComparer.OrdinalIgnoreCase);
        var changedKeys = new List<string>();
        var changedAt = DateTime.UtcNow;

        foreach (var item in normalized)
        {
            if (!byKey.TryGetValue(item.Key, out var setting))
            {
                db.SystemSettings.Add(new SystemSetting
                {
                    Section = definition.Name,
                    Key = item.Key,
                    Value = item.Value,
                    UpdatedAtUtc = changedAt
                });
                changedKeys.Add(item.Key);
                continue;
            }

            if (setting.Key == item.Key && setting.Value == item.Value) continue;
            setting.Key = item.Key;
            setting.Value = item.Value;
            setting.UpdatedAtUtc = changedAt;
            changedKeys.Add(item.Key);
        }

        if (changedKeys.Count == 0) return CreateDto(definition, existing);

        var gradeRulesChanged = definition.Name == "grade-rules" && changedKeys.Any(key => GradeThresholdKeys.Contains(key) || GradeWeightKeys.Contains(key));
        var attendanceResultRulesChanged = definition.Name == "attendance-rules" && changedKeys.Any(AttendanceResultKeys.Contains);
        if (gradeRulesChanged || attendanceResultRulesChanged)
        {
            var gradeValues = definition.Name == "grade-rules"
                ? normalized
                : SettingsCatalog.MergeDefaults("grade-rules", await db.SystemSettings.AsNoTracking()
                    .Where(setting => setting.Section == "grade-rules")
                    .Select(setting => new KeyValuePair<string, string>(setting.Key, setting.Value))
                    .ToListAsync(cancellationToken));
            var attendanceValues = definition.Name == "attendance-rules"
                ? normalized
                : SettingsCatalog.MergeDefaults("attendance-rules", await db.SystemSettings.AsNoTracking()
                    .Where(setting => setting.Section == "attendance-rules")
                    .Select(setting => new KeyValuePair<string, string>(setting.Key, setting.Value))
                    .ToListAsync(cancellationToken));
            var scale = GradeThresholds.From(gradeValues);
            var weights = GradeWeights.From(gradeValues);
            var attendanceRules = AttendanceResultRules.From(attendanceValues);
            var currentYear = await db.SystemSettings.AsNoTracking().Where(setting => setting.Section == "academic-year" && setting.Key == "currentYear").Select(setting => setting.Value).FirstOrDefaultAsync(cancellationToken) ?? "2026–2027";
            var currentTerm = await db.SystemSettings.AsNoTracking().Where(setting => setting.Section == "semester" && setting.Key == "currentTerm").Select(setting => setting.Value).FirstOrDefaultAsync(cancellationToken) ?? "Semester 1";
            var grades = await db.GradeRecords.Where(item => item.AcademicYear == currentYear && item.Term == currentTerm).ToListAsync(cancellationToken);
            var sessions = await db.ClassSessionRecords.AsNoTracking().Where(item => item.AcademicYear == currentYear && item.Term == currentTerm).ToListAsync(cancellationToken);
            var sessionsByCourse = sessions.ToLookup(item => item.CourseId);
            foreach (var grade in grades)
            {
                if (gradeRulesChanged && changedKeys.Any(GradeWeightKeys.Contains))
                {
                    grade.AssignmentScore = Rescale(grade.AssignmentScore, grade.AssignmentMaximum, weights.Assignment);
                    grade.MidtermScore = Rescale(grade.MidtermScore, grade.MidtermMaximum, weights.Midterm);
                    grade.FinalExamScore = Rescale(grade.FinalExamScore, grade.FinalExamMaximum, weights.FinalExam);
                }
                var attendance = GradeCompositionCalculator.Attendance(sessionsByCourse[grade.CourseId], grade.StudentId, weights.Attendance, attendanceRules);
                GradeCompositionCalculator.Apply(grade, weights, scale, attendance, grade.AssignmentScore, grade.MidtermScore, grade.FinalExamScore);
                grade.UpdatedAtUtc = changedAt;
            }
        }

        var orderedKeys = changedKeys.Order(StringComparer.Ordinal).ToArray();
        db.AuditLogs.Add(new AuditLog
        {
            Type = "Settings",
            Subject = definition.Name,
            Action = "Updated",
            Details = $"Changed configuration: {string.Join(", ", orderedKeys)}"
        });
        db.Notifications.Add(new Notification
        {
            Title = "Configuration updated",
            Message = $"{definition.Name} settings now apply across the institute.",
            Severity = "Info"
        });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
        if (definition.Name is "academic-year" or "semester")
            await calendar.ApplyForCurrentDateAsync(cancellationToken);
        return await GetAsync(definition.Name, cancellationToken);
    }

    private static SettingsDto CreateDto(SettingsSectionDefinition definition, IReadOnlyCollection<SystemSetting> stored)
    {
        var allowed = stored.Where(setting => definition.SettingsByKey.ContainsKey(setting.Key)).ToList();
        return new SettingsDto(
            definition.Name,
            SettingsCatalog.MergeDefaults(definition.Name, allowed.Select(setting => new KeyValuePair<string, string>(setting.Key, setting.Value))),
            SettingsCatalog.IsConfigured(definition.Name, allowed.Select(setting => setting.Key)),
            allowed.Count == 0 ? null : allowed.Max(setting => setting.UpdatedAtUtc));
    }

    private static decimal Rescale(decimal score, decimal oldMaximum, decimal newMaximum) =>
        oldMaximum <= 0 ? 0 : decimal.Round(score / oldMaximum * newMaximum, 2, MidpointRounding.AwayFromZero);

}
