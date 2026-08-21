namespace InstituteManagement.Application.Abstractions;

public interface IGradeService
{
    Task SubmitAsync(Guid studentId, Guid courseId, decimal score, CancellationToken cancellationToken);
}
