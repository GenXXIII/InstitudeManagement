namespace InstituteManagement.Application.DTOs;

public sealed record DepartmentOperationDto(Guid Id, string Department, string Head, int Students, int Teachers, int Courses, string Status);
