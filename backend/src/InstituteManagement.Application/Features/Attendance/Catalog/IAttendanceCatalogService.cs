namespace InstituteManagement.Application.Features.Attendance;

public interface IAttendanceCatalogService
{
    Task<IReadOnlyList<AttendanceResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<AttendanceResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<AttendanceResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
