namespace InstituteManagement.Application.DTOs;

public sealed record TeacherOperationDto(Guid Id, string Teacher, string TeacherNumber, string Department, string Status);
