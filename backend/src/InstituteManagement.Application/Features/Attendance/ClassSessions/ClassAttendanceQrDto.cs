namespace InstituteManagement.Application.Features.Attendance.ClassSessions;

public sealed record ClassAttendanceQrDto(
    string Payload,
    DateTime GeneratedAtUtc,
    DateTime ExpiresAtUtc);

public sealed record ClassAttendanceCheckInDto(
    Guid ScheduleEntryId,
    Guid StudentId,
    string Status,
    DateOnly Date,
    TimeOnly? CheckedInAt,
    string Method);
