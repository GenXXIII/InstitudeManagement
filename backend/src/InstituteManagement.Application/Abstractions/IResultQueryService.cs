using InstituteManagement.Application.DTOs.Results;

namespace InstituteManagement.Application.Abstractions;

public interface IResultQueryService
{
    Task<IReadOnlyList<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, CancellationToken cancellationToken);
}
