namespace InstituteManagement.Application.Features.Management.Teachers;

public sealed record TeacherResponseDto(Guid Id, TeacherValuesDto Values);

public sealed record TeacherValuesDto(
    string PhotoDataUrl,
    string TeacherCode,
    string Name,
    string Email,
    string DepartmentId,
    string Department,
    string Status,
    string CreateAt);
