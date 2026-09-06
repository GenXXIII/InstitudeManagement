namespace InstituteManagement.Application.Features.Operations;

public sealed record StudentOperationDto(Guid Id, string Student, string StudentCode, string OperationCode, string Department, int Year, string Shift, string AttendanceStatus);
