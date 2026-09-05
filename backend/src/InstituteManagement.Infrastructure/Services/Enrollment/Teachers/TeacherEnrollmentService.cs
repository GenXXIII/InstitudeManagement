using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Teachers;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Teachers;

internal sealed class TeacherEnrollmentService(
    EnrollmentSettingsReader settings,
    EnrollmentChangeCommitter committer,
    TeacherAssignmentReader reader,
    TeacherAssignmentEditor editor) : ITeacherAssignmentService
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken) =>
        await reader.GetAsync(search, departmentId, year, await settings.CurrentPeriodAsync(cancellationToken), cancellationToken);

    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid teacherId,
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var result = await editor.UpdateAsync(
            teacherId,
            values,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);
        await committer.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> RemoveAsync(Guid teacherId, CancellationToken cancellationToken)
    {
        var removed = await editor.RemoveAsync(
            teacherId,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);
        if (removed)
        {
            await committer.CommitAsync(cancellationToken);
        }

        return removed;
    }
}
