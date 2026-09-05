using MediatR;

namespace InstituteManagement.Application.Features.Timetable.GetTimetableEntries;

public sealed record GetTimetableEntriesQuery(string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<TimetableResponseDto>>;
