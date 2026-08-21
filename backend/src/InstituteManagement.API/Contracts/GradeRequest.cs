namespace InstituteManagement.API.Contracts;

public sealed record GradeRequest(Guid StudentId, Guid CourseId, decimal Score);
