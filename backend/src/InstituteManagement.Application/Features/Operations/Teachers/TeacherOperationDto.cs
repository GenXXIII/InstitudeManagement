namespace InstituteManagement.Application.Features.Operations;

public sealed record TeacherOperationDto(Guid Id, string Teacher, string TeacherCode, string OperationCode, string Department, string Status);
