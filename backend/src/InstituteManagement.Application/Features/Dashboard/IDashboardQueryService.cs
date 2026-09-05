namespace InstituteManagement.Application.Features.Dashboard;

public interface IDashboardQueryService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken);
}
