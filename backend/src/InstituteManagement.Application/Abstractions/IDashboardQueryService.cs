using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Application.Abstractions;

public interface IDashboardQueryService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken);
}
