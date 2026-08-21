namespace InstituteManagement.Application.DTOs.Management.Teachers;

public sealed record TeacherResponseDto(Guid Id, TeacherValuesDto Values) : IManagementItemDto
{
    object IManagementItemDto.Values => Values;
}

public sealed record TeacherValuesDto(
    string PhotoDataUrl,
    string Number,
    string Name,
    string Email,
    string DepartmentId,
    string Department,
    string Status);
