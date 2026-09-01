using InstituteManagement.Application.Settings;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class SettingsCatalogSeeder
{
    public static async Task SeedMissingAsync(InstituteDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = await db.SystemSettings.AsNoTracking()
            .Select(setting => new { setting.Section, setting.Key })
            .ToListAsync(cancellationToken);
        var existingKeys = existing
            .Select(setting => CompositeKey(setting.Section, setting.Key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hadGradeRules = existing.Any(setting => setting.Section.Equals("grade-rules", StringComparison.OrdinalIgnoreCase));
        var now = DateTime.UtcNow;
        var missing = new List<SystemSetting>();

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

        if (missing.Count == 0) return;
        db.SystemSettings.AddRange(missing);
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

    private static string CompositeKey(string section, string key) => $"{section}\u001f{key}";
}
