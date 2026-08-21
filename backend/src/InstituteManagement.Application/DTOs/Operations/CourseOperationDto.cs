namespace InstituteManagement.Application.DTOs;

public sealed record CourseOperationDto(Guid Id, string Course, string Code, string Teacher, string Department, int Capacity, string Status);
