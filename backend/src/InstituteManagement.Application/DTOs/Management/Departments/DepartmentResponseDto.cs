namespace InstituteManagement.Application.DTOs.Management.Departments;

public sealed record DepartmentResponseDto(Guid Id, DepartmentValuesDto Values) : IManagementItemDto
{
    object IManagementItemDto.Values => Values;
}

public sealed record DepartmentValuesDto(
    string Code,
    string Name,
    string HeadTeacherId,
    string Head,
    string Status);
