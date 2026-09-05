namespace InstituteManagement.API.Contracts.Grades;

public sealed record SubmitGradeRequest(Guid StudentId, Guid CourseId, decimal Score);
