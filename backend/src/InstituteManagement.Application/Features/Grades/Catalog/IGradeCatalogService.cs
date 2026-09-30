using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Features.Grades;

public interface IGradeCatalogService
{
    Task<PagedResult<GradeResponseDto>> GetAsync(string? search, Guid? departmentId, int? year, Guid? teacherId, string? groupBy, string? status, PageRequest page, CancellationToken cancellationToken);
    Task<GradeResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<GradeResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
