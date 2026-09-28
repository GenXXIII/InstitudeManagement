namespace InstituteManagement.Application.Features.Enrollment.Students.Progression;

public sealed record StudentEnrollmentProgressionRequest(
    Guid StudentId,
    Guid StudentEnrollmentId,
    string AcademicYear,
    string Semester,
    string FinancialAccountCode,
    string FinanceStatus);

public interface IStudentEnrollmentProgression
{
    Task<string> ReleaseAsync(
        StudentEnrollmentProgressionRequest request,
        CancellationToken cancellationToken);
}
