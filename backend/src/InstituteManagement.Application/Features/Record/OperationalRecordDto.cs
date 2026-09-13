namespace InstituteManagement.Application.Features.Record;

public sealed record OperationalRecordGradeDto(
    Guid CourseId,
    string GradeCode,
    string CourseCode,
    string CourseName,
    decimal Score,
    string Grade,
    decimal AttendanceScore,
    decimal AttendanceMaximum,
    int AttendancePresent,
    int AttendanceSessions,
    decimal AssignmentScore,
    decimal AssignmentMaximum,
    decimal MidtermScore,
    decimal MidtermMaximum,
    decimal FinalExamScore,
    decimal FinalExamMaximum);

public sealed record OperationalRecordInsightsDto(
    int PresentCount,
    int PermissionCount,
    int AbsentCount,
    IReadOnlyList<OperationalRecordGradeDto> Grades,
    int ExpectedCourses,
    decimal TotalScore,
    decimal Average,
    string Result,
    bool IsFinal);

public sealed record OperationalRecordDto(
    Guid Id,
    string Module,
    string Subject,
    string Identifier,
    string Status,
    string Summary,
    DateTime? LastActivityAt,
    IReadOnlyList<Dictionary<string, string>> Activities,
    string ClassSessionRecordCode = "",
    string Code = "",
    string PhotoDataUrl = "",
    string Department = "",
    string AcademicYear = "",
    string Term = "",
    Guid ResourceId = default,
    OperationalRecordInsightsDto? Insights = null);
