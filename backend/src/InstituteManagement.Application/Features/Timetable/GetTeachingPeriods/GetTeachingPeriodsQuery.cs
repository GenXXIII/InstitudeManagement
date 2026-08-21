using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Timetable.GetTeachingPeriods;

public sealed record GetTeachingPeriodsQuery : IRequest<IReadOnlyList<TimetablePeriodDto>>;
