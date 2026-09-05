using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Classrooms;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Classrooms;

internal sealed class ClassroomEnrollmentService(
    EnrollmentSettingsReader settings,
    EnrollmentChangeCommitter committer,
    ClassroomAssignmentReader reader,
    ClassroomAssignmentEditor editor) : IClassroomAssignmentService
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken) =>
        await reader.GetAsync(search, departmentId, year, await settings.CurrentPeriodAsync(cancellationToken), cancellationToken);

    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid classroomId,
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var result = await editor.UpdateAsync(
            classroomId,
            values,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);
        await committer.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> RemoveAsync(Guid classroomId, CancellationToken cancellationToken)
    {
        var removed = await editor.RemoveAsync(
            classroomId,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);
        if (removed)
        {
            await committer.CommitAsync(cancellationToken);
        }

        return removed;
    }
}
