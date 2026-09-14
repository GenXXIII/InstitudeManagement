namespace InstituteManagement.Application.Features.Attendance.ClassSessions;

public sealed record ClassSessionStartDto(
    Guid Id,
    Guid ScheduleEntryId,
    Guid TeacherId,
    DateOnly SessionDate,
    DateTime StartedAtUtc);
