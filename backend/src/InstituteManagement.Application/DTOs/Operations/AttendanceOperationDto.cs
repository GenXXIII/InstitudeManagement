namespace InstituteManagement.Application.DTOs;

public sealed record AttendanceOperationDto(Guid Id, string Time, string Student, string StudentCode, string Method, string Status);
