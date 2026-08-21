namespace InstituteManagement.API.Contracts;

public sealed record AttendanceRequest(Guid StudentId, string Status);
