namespace InstituteManagement.API.Contracts.Grades;

public sealed record SubmitGradeRequest(Guid StudentId, Guid CourseId, Guid TeacherId, decimal AssignmentScore, decimal MidtermScore, decimal FinalExamScore);
