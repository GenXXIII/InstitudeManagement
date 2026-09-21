namespace InstituteManagement.API.Contracts.Results;

public sealed record PublishSemesterResultRequest(Guid StudentId, string AcademicYear, string Semester);
