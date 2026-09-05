namespace InstituteManagement.Application.Features.Dashboard;

public interface IDashboardQueryService
{
    Task<DashboardDto> GetAsync(string range, CancellationToken cancellationToken);
}
