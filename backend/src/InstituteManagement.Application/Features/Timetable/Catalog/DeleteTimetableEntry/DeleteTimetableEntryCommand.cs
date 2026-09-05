using MediatR;

namespace InstituteManagement.Application.Features.Timetable.DeleteTimetableEntry;

public sealed record DeleteTimetableEntryCommand(Guid Id) : IRequest<bool>;
