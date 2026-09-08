using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class TimetableHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Timetable";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.ScheduleEntries.AsNoTracking().ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.TimetableCode, x.Status, new { x.TimetableCode, x.Shift, dayOfWeek = x.DayOfWeek.ToString(), x.StartsAt, x.EndsAt, x.Status, x.CreateAt, x.UpdatedAtUtc })).ToList();
}
