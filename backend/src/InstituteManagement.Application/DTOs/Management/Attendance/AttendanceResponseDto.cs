namespace InstituteManagement.Application.DTOs.Management.Attendance;

public sealed record AttendanceResponseDto(Guid Id, AttendanceValuesDto Values) : IManagementItemDto
{
    object IManagementItemDto.Values => Values;
}

public sealed record AttendanceValuesDto(
    string StudentId,
    string Student,
    string Number,
    string DepartmentId,
    string Department,
    string Date,
    string CheckedInAt,
    string Status,
    string Method,
    string AcademicYear,
    string Term);
