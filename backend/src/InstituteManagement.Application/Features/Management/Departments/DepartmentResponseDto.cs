namespace InstituteManagement.Application.Features.Management.Departments;

public sealed record DepartmentResponseDto(Guid Id, DepartmentValuesDto Values);

public sealed record DepartmentValuesDto(
    string DepartmentCode,
    string Name,
    string HeadTeacherId,
    string Head,
    string Status,
    string CreateAt);
