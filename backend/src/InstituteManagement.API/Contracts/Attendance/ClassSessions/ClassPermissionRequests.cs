namespace InstituteManagement.API.Contracts.Attendance.ClassSessions;

public sealed record RequestClassPermissionRequest(DateOnly SessionDate, string Reason);
public sealed record ReviewClassPermissionRequest(Guid TeacherId, string Decision);
