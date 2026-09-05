namespace InstituteManagement.Application.Features.Grades;

public interface IGradeCatalogService
{
    Task<IReadOnlyList<GradeResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<GradeResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<GradeResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
