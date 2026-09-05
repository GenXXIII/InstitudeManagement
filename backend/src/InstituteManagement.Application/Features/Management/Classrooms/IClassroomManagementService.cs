namespace InstituteManagement.Application.Features.Management.Classrooms;

public interface IClassroomManagementService
{
    Task<IReadOnlyList<ClassroomResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<ClassroomResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<ClassroomResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
