namespace InstituteManagement.Application.Features.Administration;

public interface ISettingsService
{
    Task<IReadOnlyList<SettingsDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<SettingsDto> GetAsync(string section, CancellationToken cancellationToken);
    Task<SettingsDto> SaveAsync(string section, Dictionary<string, string> values, CancellationToken cancellationToken);
}
