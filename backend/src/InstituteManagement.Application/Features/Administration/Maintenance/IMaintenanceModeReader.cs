namespace InstituteManagement.Application.Features.Administration.Maintenance;

public sealed record MaintenanceModeState(bool Enabled, string? Message);

public interface IMaintenanceModeReader
{
    Task<MaintenanceModeState> GetAsync(CancellationToken cancellationToken);
}
