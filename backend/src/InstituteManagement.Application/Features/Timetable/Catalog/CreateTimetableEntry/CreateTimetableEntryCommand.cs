using MediatR;

namespace InstituteManagement.Application.Features.Timetable.CreateTimetableEntry;

public sealed record CreateTimetableEntryCommand(Dictionary<string, string> Values) : IRequest<TimetableResponseDto>;
