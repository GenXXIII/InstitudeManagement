using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed class SettingsService(InstituteDbContext db) : ISettingsService
{
    public async Task<SettingsDto> GetAsync(string section, CancellationToken ct) => new(section, await db.SystemSettings.AsNoTracking().Where(x => x.Section == section).ToDictionaryAsync(x => x.Key, x => x.Value, ct));

    public async Task<SettingsDto> SaveAsync(string section, Dictionary<string, string> values, CancellationToken ct)
    {
        Validate(section, values);
        var existing = await db.SystemSettings.Where(x => x.Section == section).ToListAsync(ct);
        foreach (var item in values)
        {
            var setting = existing.FirstOrDefault(x => x.Key == item.Key);
            if (setting is null) db.SystemSettings.Add(new SystemSetting { Section = section, Key = item.Key, Value = item.Value });
            else { setting.Value = item.Value; setting.UpdatedAtUtc = DateTime.UtcNow; }
        }
        await db.SaveChangesAsync(ct);
        return new SettingsDto(section, values);
    }

    private static void Validate(string section, Dictionary<string, string> values)
    {
        if (section == "grade-rules")
        {
            var keys = new[] { "aMinimum", "bMinimum", "cMinimum", "dMinimum" };
            var scores = keys.Select(key => decimal.TryParse(values.GetValueOrDefault(key), out var score) ? score : throw new ArgumentException($"{key} must be numeric.")).ToArray();
            if (!(scores[0] > scores[1] && scores[1] > scores[2] && scores[2] > scores[3] && scores[3] >= 0)) throw new ArgumentException("Grade thresholds must descend from A through D.");
        }
        if (section == "attendance-rules" && int.TryParse(values.GetValueOrDefault("lateThresholdMinutes"), out var threshold) && threshold < 0) throw new ArgumentException("Late threshold cannot be negative.");
    }
}
