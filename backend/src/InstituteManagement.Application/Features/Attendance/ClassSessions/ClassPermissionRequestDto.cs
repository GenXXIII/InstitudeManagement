namespace InstituteManagement.Application.Features.Attendance.ClassSessions;

public sealed record ClassPermissionRequestDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentPublicId,
    Guid? TeacherId,
    string TeacherName,
    DateOnly SessionDate,
    string Reason,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? ReviewedAtUtc);
