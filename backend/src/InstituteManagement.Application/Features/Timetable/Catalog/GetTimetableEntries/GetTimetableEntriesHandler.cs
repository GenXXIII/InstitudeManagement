using MediatR;

namespace InstituteManagement.Application.Features.Timetable.GetTimetableEntries;

public sealed class GetTimetableEntriesHandler(ITimetableCatalogService service) : IRequestHandler<GetTimetableEntriesQuery, IReadOnlyList<TimetableResponseDto>>
{
    public Task<IReadOnlyList<TimetableResponseDto>> Handle(GetTimetableEntriesQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, cancellationToken);
}
