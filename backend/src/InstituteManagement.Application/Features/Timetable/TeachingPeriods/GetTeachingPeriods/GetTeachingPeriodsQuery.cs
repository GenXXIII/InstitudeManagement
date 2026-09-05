using InstituteManagement.Application.Features.Operations;
using MediatR;

namespace InstituteManagement.Application.Features.Timetable.GetTeachingPeriods;

public sealed record GetTeachingPeriodsQuery : IRequest<IReadOnlyList<TimetablePeriodDto>>;
