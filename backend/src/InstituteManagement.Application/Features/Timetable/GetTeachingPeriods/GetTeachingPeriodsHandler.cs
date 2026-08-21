using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using MediatR;

namespace InstituteManagement.Application.Features.Timetable.GetTeachingPeriods;

public sealed class GetTeachingPeriodsHandler : IRequestHandler<GetTeachingPeriodsQuery, IReadOnlyList<TimetablePeriodDto>>
{
    public Task<IReadOnlyList<TimetablePeriodDto>> Handle(GetTeachingPeriodsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TimetablePeriodDto>>(AcademicTimetablePolicy.All
            .Select(period => new TimetablePeriodDto(period.DayGroup, period.Session, period.StartsAt.ToString("HH:mm"), period.EndsAt.ToString("HH:mm")))
            .ToList());
}
