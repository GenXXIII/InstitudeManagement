using InstituteManagement.Application.Features.Results;

namespace InstituteManagement.Application.Features.Results;

public interface IResultQueryService
{
    Task<IReadOnlyList<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, CancellationToken cancellationToken);
}
