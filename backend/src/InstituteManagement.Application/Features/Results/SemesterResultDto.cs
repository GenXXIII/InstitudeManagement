namespace InstituteManagement.Application.Features.Results;

public sealed record CourseResultDto(Guid CourseId, string CourseCode, string Name, decimal? Score, string Grade, bool IsApproved);

public sealed record SemesterResultDto(
    Guid StudentId,
    string StudentCode,
    string ResultCode,
    string FullName,
    Guid DepartmentId,
    string Department,
    int Year,
    string Shift,
    string AcademicYear,
    string Semester,
    int PresentCount,
    int AbsentCount,
    int PermissionCount,
    decimal AttendanceScore,
    decimal AttendanceMaximum,
    string AttendanceGrade,
    IReadOnlyList<CourseResultDto> Grades,
    int ExpectedCourseCount,
    int TotalCourses,
    decimal TotalScore,
    decimal Average,
    string OverallGrade,
    string TotalGrade,
    string PublicationStatus,
    bool IsPublished,
    DateTime? PublishedAtUtc);
