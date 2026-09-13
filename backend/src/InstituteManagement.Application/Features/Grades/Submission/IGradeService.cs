namespace InstituteManagement.Application.Features.Grades;

public interface IGradeService
{
    Task SubmitAsync(Guid studentId, Guid courseId, decimal assignmentScore, decimal midtermScore, decimal finalExamScore, CancellationToken cancellationToken);
}
