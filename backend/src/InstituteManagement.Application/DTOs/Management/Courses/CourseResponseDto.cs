namespace InstituteManagement.Application.DTOs.Management.Courses;

public sealed record CourseResponseDto(Guid Id, CourseValuesDto Values) : IManagementItemDto
{
    object IManagementItemDto.Values => Values;
}

public sealed record CourseValuesDto(
    string CourseCode,
    string Name,
    string DepartmentId,
    string Department,
    string TeacherId,
    string Teacher,
    string Capacity,
    string Status,
    string CreateAt);
