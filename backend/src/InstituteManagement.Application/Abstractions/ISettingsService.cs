using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Application.Abstractions;

public interface ISettingsService
{
    Task<SettingsDto> GetAsync(string section, CancellationToken cancellationToken);
    Task<SettingsDto> SaveAsync(string section, Dictionary<string, string> values, CancellationToken cancellationToken);
}
