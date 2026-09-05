namespace InstituteManagement.Application.Features.Results;

public sealed record CourseResultDto(Guid CourseId, string CourseCode, string Name, decimal Score, string Grade);

public sealed record SemesterResultDto(
    Guid StudentId,
    string StudentCode,
    string FullName,
    Guid DepartmentId,
    string Department,
    int Year,
    string AcademicYear,
    string Semester,
    int PresentCount,
    int AbsentCount,
    int PermissionCount,
    IReadOnlyList<CourseResultDto> Grades,
    int TotalCourses,
    decimal TotalScore,
    decimal Average,
    string TotalGrade);
