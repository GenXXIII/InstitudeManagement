using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Application.Settings;
using InstituteManagement.Domain.Entities;
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
        "aPlusMinimum", "aMinimum", "bPlusMinimum", "bMinimum",
        "cPlusMinimum", "cMinimum", "dMinimum"
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

        if (definition.Name is "institute" or "system")
            await SynchronizeTimeZoneAsync(definition.Name, normalized["timeZone"], changedAt, changedKeys, cancellationToken);

        if (changedKeys.Count == 0) return CreateDto(definition, existing);

        if (definition.Name == "grade-rules" && changedKeys.Any(GradeThresholdKeys.Contains))
        {
            var scale = GradeThresholds.From(normalized);
            foreach (var grade in await db.GradeRecords.ToListAsync(cancellationToken))
            {
                grade.LetterGrade = scale.Letter(grade.Score);
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

    private async Task SynchronizeTimeZoneAsync(
        string sourceSection,
        string timeZone,
        DateTime changedAt,
        ICollection<string> changedKeys,
        CancellationToken cancellationToken)
    {
        var targetSection = sourceSection == "institute" ? "system" : "institute";
        var target = await db.SystemSettings.FirstOrDefaultAsync(
            setting => setting.Section == targetSection && setting.Key == "timeZone",
            cancellationToken);
        if (target is null)
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Section = targetSection,
                Key = "timeZone",
                Value = timeZone,
                UpdatedAtUtc = changedAt
            });
            changedKeys.Add($"{targetSection}.timeZone");
        }
        else if (target.Value != timeZone)
        {
            target.Value = timeZone;
            target.UpdatedAtUtc = changedAt;
            changedKeys.Add($"{targetSection}.timeZone");
        }
    }
}
