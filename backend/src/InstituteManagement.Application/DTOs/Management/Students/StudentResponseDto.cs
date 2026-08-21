namespace InstituteManagement.Application.DTOs.Management.Students;

public sealed record StudentResponseDto(Guid Id, StudentValuesDto Values) : IManagementItemDto
{
    object IManagementItemDto.Values => Values;
}

public sealed record StudentValuesDto(
    string PhotoDataUrl,
    string Number,
    string Name,
    string Email,
    string DepartmentId,
    string Department,
    string Year,
    string Status);
