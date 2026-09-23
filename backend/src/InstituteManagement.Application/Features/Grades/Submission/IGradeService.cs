namespace InstituteManagement.Application.Features.Grades;

public interface IGradeService
{
    Task SubmitAsync(Guid studentId, Guid courseId, decimal assignmentScore, decimal midtermScore, decimal finalExamScore, CancellationToken cancellationToken);
    Task SubmitAsync(Guid studentId, Guid courseId, Guid teacherId, decimal assignmentScore, decimal midtermScore, decimal finalExamScore, CancellationToken cancellationToken);
    Task RequestCourseSubmissionAsync(Guid teacherId, Guid courseId, IReadOnlyList<GradeStudentScore> students, CancellationToken cancellationToken);
    Task SubmitAuthorizedAsync(Guid gradeId, Guid teacherId, CancellationToken cancellationToken);
    Task SubmitAuthorizedCourseAsync(Guid gradeId, Guid teacherId, IReadOnlyList<GradeStudentScore> students, CancellationToken cancellationToken);
    Task RequestCourseResubmissionAsync(Guid gradeId, Guid teacherId, string note, CancellationToken cancellationToken);
    Task ReviewAsync(Guid gradeId, string decision, string note, CancellationToken cancellationToken);
}

public sealed record GradeStudentScore(Guid StudentId, decimal AssignmentScore, decimal MidtermScore, decimal FinalExamScore);
