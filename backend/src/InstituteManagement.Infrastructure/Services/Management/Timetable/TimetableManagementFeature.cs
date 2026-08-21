using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Timetable;

public sealed class TimetableManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "timetable";
    public override async Task<IReadOnlyList<CatalogItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct) => (await Db.ScheduleEntries.AsNoTracking().Include(x => x.Course).ThenInclude(x => x!.Department).Include(x => x.Teacher).Include(x => x.Classroom).Where(x => x.Status != "Cancelled" && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId)).ToListAsync(ct)).Where(x => Matches(search, x.Course?.Name, x.Teacher?.FullName, x.Classroom?.Code, x.Status)).Select(x => Item(x.Id, ("courseId", x.CourseId.ToString()), ("course", x.Course?.Name ?? "—"), ("teacherId", x.TeacherId.ToString()), ("teacher", x.Teacher?.FullName ?? "—"), ("classroomId", x.ClassroomId.ToString()), ("classroom", x.Classroom?.Code ?? "—"), ("departmentId", x.Course?.DepartmentId.ToString() ?? ""), ("department", x.Course?.Department?.Name ?? "—"), ("dayOfWeek", x.DayOfWeek.ToString()), ("startsAt", x.StartsAt.ToString("HH:mm")), ("endsAt", x.EndsAt.ToString("HH:mm")), ("status", x.Status))).ToList();
    public override async Task<CatalogItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct) { var entity = new ScheduleEntry(); await ApplyAsync(entity, values, ct); return await SaveCreatedAsync(entity, values, ct); }
    public override async Task<CatalogItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct) { var entity = await RequiredEntityAsync(Db.ScheduleEntries, id, ct); await ApplyAsync(entity, values, ct); Touch(entity); return await SaveUpdatedAsync(id, values, ct); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.ScheduleEntries.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((ScheduleEntry)entity).Status = "Cancelled"; Touch(entity); }
    private async Task ApplyAsync(ScheduleEntry entry, Dictionary<string, string> values, CancellationToken ct)
    {
        entry.CourseId = await RelatedIdAsync<Course>(values, "courseId", ct); entry.TeacherId = await RelatedIdAsync<Teacher>(values, "teacherId", ct); entry.ClassroomId = await RelatedIdAsync<Classroom>(values, "classroomId", ct);
        var course = await Db.Courses.FindAsync([entry.CourseId], ct) ?? throw new KeyNotFoundException("Course not found."); var teacher = await Db.Teachers.FindAsync([entry.TeacherId], ct) ?? throw new KeyNotFoundException("Teacher not found."); var room = await Db.Classrooms.FindAsync([entry.ClassroomId], ct) ?? throw new KeyNotFoundException("Classroom not found.");
        if (!course.IsActive || teacher.Status == "Inactive" || room.Status == "Inactive" || teacher.DepartmentId != course.DepartmentId || room.DepartmentId != course.DepartmentId) throw new InvalidOperationException("Course, teacher, and classroom must be active and belong to the same department.");
        entry.DayOfWeek = Enum.Parse<DayOfWeek>(Required(values, "dayOfWeek")); entry.StartsAt = TimeOnly.Parse(Required(values, "startsAt")); entry.EndsAt = TimeOnly.Parse(Required(values, "endsAt")); if (entry.EndsAt <= entry.StartsAt) throw new ArgumentException("Timetable end time must be after start time.");
        if (await Db.ScheduleEntries.AnyAsync(x => x.Id != entry.Id && x.Status != "Cancelled" && x.DayOfWeek == entry.DayOfWeek && x.StartsAt < entry.EndsAt && entry.StartsAt < x.EndsAt && (x.TeacherId == entry.TeacherId || x.ClassroomId == entry.ClassroomId), ct)) throw new InvalidOperationException("Teacher or classroom is already scheduled during this time.");
        entry.Status = Get(values, "status", "Upcoming");
    }
}
