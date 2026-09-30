using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Features.Management.Teachers;

public interface ITeacherManagementService
{
    Task<PagedResult<TeacherResponseDto>> GetAsync(string? search, Guid? departmentId, Guid? profileId, PageRequest page, CancellationToken cancellationToken);
    Task<TeacherResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<TeacherResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
