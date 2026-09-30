using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Features.Attendance;

public interface IAttendanceCatalogService
{
    Task<PagedResult<AttendanceResponseDto>> GetAsync(string? search, Guid? departmentId, int? year, string? shift, PageRequest page, CancellationToken cancellationToken);
    Task<AttendanceResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<AttendanceResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
