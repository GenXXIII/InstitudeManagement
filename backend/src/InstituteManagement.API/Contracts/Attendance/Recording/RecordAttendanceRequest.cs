namespace InstituteManagement.API.Contracts.Attendance;

public sealed record RecordAttendanceRequest(Guid StudentId, string Status);
