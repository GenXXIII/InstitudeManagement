using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

internal static class ClassSessionStartStateReader
{
    public static async Task<HashSet<Guid>> GetStartedScheduleIdsAsync(
        InstituteDbContext db,
        DateOnly sessionDate,
        IEnumerable<Guid> scheduleEntryIds,
        CancellationToken cancellationToken)
    {
        var ids = scheduleEntryIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        var started = await db.ClassSessionStarts.AsNoTracking()
            .Where(item => item.SessionDate == sessionDate && ids.Contains(item.ScheduleEntryId))
            .Select(item => item.ScheduleEntryId)
            .ToListAsync(cancellationToken);
        return started.ToHashSet();
    }
}
