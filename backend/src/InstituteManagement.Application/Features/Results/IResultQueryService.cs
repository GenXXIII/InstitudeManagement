using InstituteManagement.Application.Features.Results;

namespace InstituteManagement.Application.Features.Results;

public interface IResultQueryService
{
    Task<IReadOnlyList<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, CancellationToken cancellationToken);
    Task<IReadOnlyList<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, Guid? studentId, bool publishedOnly, CancellationToken cancellationToken);
    Task PublishAsync(Guid studentId, string academicYear, string semester, CancellationToken cancellationToken);
    Task<int> PublishAllAsync(Guid? departmentId, int? year, CancellationToken cancellationToken);
}
