using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Timetable;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal sealed class TimetableEnrollmentService(
    EnrollmentSettingsReader settings,
    EnrollmentChangeCommitter committer,
    TimetableEnrollmentReader reader,
    TimetableEnrollmentEditor editor) : ITimetableEnrollmentService
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken) =>
        await reader.GetAsync(search, departmentId, year, await settings.CurrentPeriodAsync(cancellationToken), cancellationToken);

    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid scheduleEntryId,
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var result = await editor.UpdateAsync(
            scheduleEntryId,
            values,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);
        await committer.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> RemoveAsync(Guid scheduleEntryId, CancellationToken cancellationToken)
    {
        var removed = await editor.RemoveAsync(
            scheduleEntryId,
            await settings.CurrentPeriodAsync(cancellationToken),
            cancellationToken);
        if (removed)
        {
            await committer.CommitAsync(cancellationToken);
        }

        return removed;
    }
}
