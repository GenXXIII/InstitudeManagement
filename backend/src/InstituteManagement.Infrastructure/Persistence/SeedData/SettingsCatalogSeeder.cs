using InstituteManagement.Application.Features.Administration.Settings;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class SettingsCatalogSeeder
{
    private static readonly string[] LegacyNotificationCodeKeys =
    [
        "notificationCodePrefix", "historyCodePrefix", "codeIncludeYear",
        "codeStartingNumber", "codePaddingWidth", "codeSeparator"
    ];
    private static readonly string[] ObsoleteAlertCodeKeys =
    [
        "alertManagementPrefix", "alertEnrollmentPrefix", "alertOperationPrefix", "alertRecordPrefix", "alertHistoryPrefix"
    ];

    public static async Task SeedMissingAsync(InstituteDbContext db, CancellationToken cancellationToken = default)
    {
        await MoveNotificationCodeSettingsAsync(db, cancellationToken);
        await RemoveObsoleteAlertCodeSettingsAsync(db, cancellationToken);
        var existing = await db.SystemSettings.AsNoTracking()
            .Select(setting => new { setting.Section, setting.Key })
            .ToListAsync(cancellationToken);
        if (existing.Count == 0) return;

        var existingKeys = existing
            .Select(setting => CompositeKey(setting.Section, setting.Key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hadGradeRules = existing.Any(setting => setting.Section.Equals("grade-rules", StringComparison.OrdinalIgnoreCase));
        var now = DateTime.UtcNow;
        var missing = new List<SystemSetting>();
        var obsoleteInstituteRegionalSettings = await db.SystemSettings
            .Where(setting => setting.Section == "institute" && (setting.Key == "timeZone" || setting.Key == "dateFormat"))
            .ToListAsync(cancellationToken);

        foreach (var section in SettingsCatalog.Sections)
            foreach (var setting in section.Settings)
            {
                if (!existingKeys.Add(CompositeKey(section.Name, setting.Key))) continue;
                missing.Add(new SystemSetting
                {
                    Section = section.Name,
                    Key = setting.Key,
                    Value = setting.DefaultValue,
                    CreateAt = now,
                    UpdatedAtUtc = now
                });
            }

        if (missing.Count == 0 && obsoleteInstituteRegionalSettings.Count == 0) return;
        if (missing.Count > 0) db.SystemSettings.AddRange(missing);
        if (obsoleteInstituteRegionalSettings.Count > 0) db.SystemSettings.RemoveRange(obsoleteInstituteRegionalSettings);
        if (!hadGradeRules)
        {
            var scale = GradeThresholds.From(SettingsCatalog.Defaults("grade-rules"));
            foreach (var grade in await db.GradeRecords.ToListAsync(cancellationToken))
            {
                grade.LetterGrade = scale.Letter(grade.Score);
                grade.UpdatedAtUtc = now;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task MoveNotificationCodeSettingsAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var legacy = await db.SystemSettings
            .Where(setting => setting.Section == "notifications" && LegacyNotificationCodeKeys.Contains(setting.Key))
            .ToListAsync(cancellationToken);
        if (legacy.Count == 0) return;

        var codeFormatKeys = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "code-formats")
            .Select(setting => setting.Key)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var setting in legacy)
        {
            var prefixBelongsInCodeFormats = setting.Key is "notificationCodePrefix" or "historyCodePrefix";
            if (prefixBelongsInCodeFormats && codeFormatKeys.Add(setting.Key)) setting.Section = "code-formats";
            else db.SystemSettings.Remove(setting);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RemoveObsoleteAlertCodeSettingsAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var obsolete = await db.SystemSettings
            .Where(setting => setting.Section == "code-formats" && ObsoleteAlertCodeKeys.Contains(setting.Key))
            .ToListAsync(cancellationToken);
        if (obsolete.Count == 0) return;
        db.SystemSettings.RemoveRange(obsolete);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string CompositeKey(string section, string key) => $"{section}\u001f{key}";
}
