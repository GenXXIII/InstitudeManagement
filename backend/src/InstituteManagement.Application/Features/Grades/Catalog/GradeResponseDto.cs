namespace InstituteManagement.Application.Features.Grades;

public sealed record GradeResponseDto(Guid Id, GradeValuesDto Values);

public sealed record GradeValuesDto(
    string GradeCode,
    string StudentId,
    string Student,
    string CourseId,
    string Course,
    string DepartmentId,
    string Department,
    string AttendanceScore,
    string AttendanceMaximum,
    string AttendancePresent,
    string AttendanceSessions,
    string AssignmentScore,
    string AssignmentMaximum,
    string MidtermScore,
    string MidtermMaximum,
    string FinalExamScore,
    string FinalExamMaximum,
    string Score,
    string Grade,
    string AcademicYear,
    string Term,
    string CreateAt);
