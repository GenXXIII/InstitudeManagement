namespace InstituteManagement.Application.Features.Attendance.ClassSessions;

public sealed record ClassAttendanceCheckInDto(
    Guid ScheduleEntryId,
    Guid StudentId,
    string Status,
    DateOnly Date,
    TimeOnly? CheckedInAt,
    string Method);
