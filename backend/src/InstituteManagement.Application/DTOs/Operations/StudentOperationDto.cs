namespace InstituteManagement.Application.DTOs;

public sealed record StudentOperationDto(Guid Id, string Student, string StudentCode, string Department, int Year, string AttendanceStatus);
