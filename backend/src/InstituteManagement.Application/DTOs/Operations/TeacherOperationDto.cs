namespace InstituteManagement.Application.DTOs;

public sealed record TeacherOperationDto(Guid Id, string Teacher, string TeacherCode, string Department, string Status);
