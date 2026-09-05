namespace InstituteManagement.Application.Features.Grades;

public interface IGradeService
{
    Task SubmitAsync(Guid studentId, Guid courseId, decimal score, CancellationToken cancellationToken);
}
