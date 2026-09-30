using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Features.Management.Students;

public interface IStudentManagementService
{
    Task<PagedResult<StudentResponseDto>> GetAsync(string? search, Guid? departmentId, Guid? profileId, PageRequest page, CancellationToken cancellationToken);
    Task<StudentResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<StudentResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
