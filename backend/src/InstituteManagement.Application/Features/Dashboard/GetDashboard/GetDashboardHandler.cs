using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Dashboard.GetDashboard;

public sealed class GetDashboardHandler(IDashboardQueryService service) : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken) => service.GetAsync(cancellationToken);
}
