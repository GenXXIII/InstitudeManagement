using InstituteManagement.Application.Features.Administration.Maintenance;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration.Maintenance;

public sealed class MaintenanceModeReader(InstituteDbContext db) : IMaintenanceModeReader
{
    public async Task<MaintenanceModeState> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "system" &&
                (setting.Key == "maintenanceEnabled" || setting.Key == "maintenanceMessage"))
            .ToDictionaryAsync(
                setting => setting.Key,
                setting => setting.Value,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        var enabled = settings.TryGetValue("maintenanceEnabled", out var enabledValue) &&
            bool.TryParse(enabledValue, out var parsedEnabled) &&
            parsedEnabled;

        return new MaintenanceModeState(enabled, settings.GetValueOrDefault("maintenanceMessage"));
    }
}
