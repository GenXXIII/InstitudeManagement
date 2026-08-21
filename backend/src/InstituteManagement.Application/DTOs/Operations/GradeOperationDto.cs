namespace InstituteManagement.Application.DTOs;

public sealed record GradeOperationDto(Guid Id, string Student, string Course, decimal Score, string Grade, string Term);
