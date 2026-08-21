namespace InstituteManagement.Application.DTOs;

public sealed record StudentOperationDto(Guid Id, string Student, string StudentNumber, string Department, int Year, string AttendanceStatus);
