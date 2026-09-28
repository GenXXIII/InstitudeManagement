namespace InstituteManagement.API.Contracts.Attendance.ClassSessions;

public sealed record TeacherClassAttendanceCheckInRequest(Guid TeacherId, string QrPayload);
