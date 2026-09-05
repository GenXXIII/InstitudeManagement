namespace InstituteManagement.Application.Features.Attendance;

public sealed record AttendanceResponseDto(Guid Id, AttendanceValuesDto Values);

public sealed record AttendanceValuesDto(
    string AttendanceCode,
    string StudentId,
    string Student,
    string StudentCode,
    string DepartmentId,
    string Department,
    string Date,
    string CheckedInAt,
    string Status,
    string Method,
    string AcademicYear,
    string Term,
    string CreateAt);
