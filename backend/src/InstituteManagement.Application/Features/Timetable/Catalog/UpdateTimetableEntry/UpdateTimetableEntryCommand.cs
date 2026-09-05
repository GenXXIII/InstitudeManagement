using MediatR;

namespace InstituteManagement.Application.Features.Timetable.UpdateTimetableEntry;

public sealed record UpdateTimetableEntryCommand(Guid Id, Dictionary<string, string> Values) : IRequest<TimetableResponseDto>;
