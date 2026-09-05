using InstituteManagement.Application.Features.Dashboard;
using MediatR;

namespace InstituteManagement.Application.Features.Dashboard.GetDashboard;

public sealed record GetDashboardQuery(string Range) : IRequest<DashboardDto>;
