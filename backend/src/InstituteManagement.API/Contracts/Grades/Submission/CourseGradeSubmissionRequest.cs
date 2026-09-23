namespace InstituteManagement.API.Contracts.Grades;

public sealed record CourseGradeSubmissionRequest(
    Guid TeacherId,
    Guid CourseId,
    IReadOnlyList<CourseGradeStudentScoreRequest> Students);

public sealed record CourseGradeStudentScoreRequest(
    Guid StudentId,
    decimal AssignmentScore,
    decimal MidtermScore,
    decimal FinalExamScore);

public sealed record SubmitAuthorizedCourseGradesRequest(
    Guid TeacherId,
    IReadOnlyList<CourseGradeStudentScoreRequest>? Students);

public sealed record CourseGradeResubmissionRequest(Guid TeacherId, string Note);
