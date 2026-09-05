using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Courses;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Courses;

internal sealed class CourseEnrollmentService(
    EnrollmentSettingsReader settings,
    EnrollmentChangeCommitter committer,
    CourseAssignmentReader reader,
    CourseAssignmentEditor editor) : ICourseAssignmentService
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken) =>
        await reader.GetAsync(search, departmentId, year, await settings.CurrentPeriodAsync(cancellationToken), cancellationToken);

    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid courseId,
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var result = await editor.UpdateAsync(
            courseId,
            values,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);
        await committer.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> RemoveAsync(Guid courseId, CancellationToken cancellationToken)
    {
        var removed = await editor.RemoveAsync(
            courseId,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);
        if (removed)
        {
            await committer.CommitAsync(cancellationToken);
        }

        return removed;
    }
}
