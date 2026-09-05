namespace InstituteManagement.Application.Features.Operations;

public sealed record TeacherOperationDto(Guid Id, string Teacher, string TeacherCode, string EnrollmentCode, string Department, string Status);
