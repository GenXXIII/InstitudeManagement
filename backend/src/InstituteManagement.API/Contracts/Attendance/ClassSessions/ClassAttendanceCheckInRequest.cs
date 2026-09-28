namespace InstituteManagement.API.Contracts.Attendance.ClassSessions;

public sealed record ClassAttendanceCheckInRequest(Guid StudentId, string QrPayload);
