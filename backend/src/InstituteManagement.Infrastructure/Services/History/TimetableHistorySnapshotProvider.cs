using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class TimetableHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Timetable";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.ScheduleEntries.AsNoTracking().Include(x => x.Course).ThenInclude(x => x!.Department).Include(x => x.Teacher).Include(x => x.Classroom).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.Course?.Name ?? x.Id.ToString(), x.Status, new { x.CourseId, course = x.Course?.Name, department = x.Course?.Department?.Name, x.TeacherId, teacher = x.Teacher?.FullName, x.ClassroomId, classroom = x.Classroom?.Code, x.YearLevel, dayOfWeek = x.DayOfWeek.ToString(), x.StartsAt, x.EndsAt, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc })).ToList();
}
