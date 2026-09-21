namespace InstituteManagement.Application.Features.Grades;

public interface IGradeService
{
    Task SubmitAsync(Guid studentId, Guid courseId, decimal assignmentScore, decimal midtermScore, decimal finalExamScore, CancellationToken cancellationToken);
    Task SubmitAsync(Guid studentId, Guid courseId, Guid teacherId, decimal assignmentScore, decimal midtermScore, decimal finalExamScore, CancellationToken cancellationToken);
    Task ReviewAsync(Guid gradeId, string decision, string note, CancellationToken cancellationToken);
    Task RequestResubmissionAsync(Guid gradeId, Guid teacherId, CancellationToken cancellationToken);
    Task AuthorizeResubmissionAsync(Guid gradeId, CancellationToken cancellationToken);
}
