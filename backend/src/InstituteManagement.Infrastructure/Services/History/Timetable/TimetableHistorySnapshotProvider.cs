using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class TimetableHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Timetable";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.ScheduleEntries.AsNoTracking().Include(x => x.Course).ThenInclude(x => x!.Department).Include(x => x.Teacher).Include(x => x.Classroom).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.Course?.Name ?? x.TimetableCode, x.Status, new { x.TimetableCode, x.CourseId, courseCode = x.Course?.CourseCode, course = x.Course?.Name, departmentCode = x.Course?.Department?.DepartmentCode, department = x.Course?.Department?.Name, x.TeacherId, teacherCode = x.Teacher?.TeacherCode, teacher = x.Teacher?.FullName, x.ClassroomId, classroomCode = x.Classroom?.ClassroomCode, classroom = x.Classroom?.ClassroomCode, x.YearLevel, dayOfWeek = x.DayOfWeek.ToString(), x.StartsAt, x.EndsAt, x.Status, x.CreateAt, x.UpdatedAtUtc })).ToList();
}
