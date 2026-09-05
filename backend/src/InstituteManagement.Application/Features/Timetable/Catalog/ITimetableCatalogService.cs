namespace InstituteManagement.Application.Features.Timetable;

public interface ITimetableCatalogService
{
    Task<IReadOnlyList<TimetableResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<TimetableResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<TimetableResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
