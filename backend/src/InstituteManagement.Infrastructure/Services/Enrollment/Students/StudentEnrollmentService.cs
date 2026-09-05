using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Students;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Students;

internal sealed class StudentEnrollmentService(
    EnrollmentSettingsReader settings,
    EnrollmentChangeCommitter committer,
    StudentEnrollmentReader reader,
    StudentEnrollmentEditor editor) : IStudentEnrollmentService
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken) =>
        await reader.GetAsync(
            search,
            departmentId,
            year,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);

    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid studentId,
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var result = await editor.UpdateAsync(
            studentId,
            values,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);
        await committer.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> RemoveAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var removed = await editor.RemoveAsync(
            studentId,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);
        if (removed)
        {
            await committer.CommitAsync(cancellationToken);
        }

        return removed;
    }
}
