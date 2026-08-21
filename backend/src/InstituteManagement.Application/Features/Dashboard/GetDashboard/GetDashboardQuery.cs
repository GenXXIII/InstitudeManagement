using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Dashboard.GetDashboard;

public sealed record GetDashboardQuery : IRequest<DashboardDto>;
