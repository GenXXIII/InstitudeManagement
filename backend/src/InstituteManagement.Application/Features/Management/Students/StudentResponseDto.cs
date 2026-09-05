namespace InstituteManagement.Application.Features.Management.Students;

public sealed record StudentResponseDto(Guid Id, StudentValuesDto Values);

public sealed record StudentValuesDto(
    string PhotoDataUrl,
    string StudentCode,
    string Name,
    string Email,
    string DepartmentId,
    string Department,
    string Year,
    string Shift,
    string Status,
    string CreateAt);
