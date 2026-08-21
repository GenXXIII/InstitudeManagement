using InstituteManagement.Application.DTOs.Management;
using InstituteManagement.Application.DTOs.Management.Timetable;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Timetable;

public sealed class TimetableManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "timetable";
    public override async Task<IReadOnlyList<IManagementItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var entries = await Db.ScheduleEntries.AsNoTracking().Include(entry => entry.Course).ThenInclude(course => course!.Department).Include(entry => entry.Teacher).Include(entry => entry.Classroom)
            .Where(entry => entry.Status != "Cancelled" && (!departmentId.HasValue || entry.Course!.DepartmentId == departmentId))
            .ToListAsync(ct);
        return entries.Where(entry => Matches(search, entry.Course?.Name, entry.Teacher?.FullName, entry.Classroom?.Code, entry.Status))
            .Select(entry => (IManagementItemDto)new TimetableResponseDto(entry.Id, new TimetableValuesDto(
                entry.CourseId.ToString(),
                entry.Course?.Name ?? "—",
                entry.TeacherId.ToString(),
                entry.Teacher?.FullName ?? "—",
                entry.ClassroomId.ToString(),
                entry.Classroom?.Code ?? "—",
                entry.Classroom?.RoomType ?? "Classroom",
                entry.Course?.DepartmentId.ToString() ?? "",
                entry.Course?.Department?.Name ?? "—",
                entry.YearLevel.ToString(),
                entry.DayOfWeek.ToString(),
                entry.StartsAt.ToString("HH:mm"),
                entry.EndsAt.ToString("HH:mm"),
                entry.Status)))
            .ToList();
    }

    public override async Task<IManagementItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct) { var entity = new ScheduleEntry(); await ApplyAsync(entity, values, ct); return await SaveCreatedAsync(entity, values, ct); }
    public override async Task<IManagementItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct) { var entity = await RequiredEntityAsync(Db.ScheduleEntries, id, ct); await ApplyAsync(entity, values, ct); Touch(entity); return await SaveUpdatedAsync(id, values, ct); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.ScheduleEntries.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((ScheduleEntry)entity).Status = "Cancelled"; Touch(entity); }
    protected override IManagementItemDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new TimetableResponseDto(id, new TimetableValuesDto(
            Get(values, "courseId"),
            Get(values, "course"),
            Get(values, "teacherId"),
            Get(values, "teacher"),
            Get(values, "classroomId"),
            Get(values, "classroom"),
            Get(values, "classroomType", "Classroom"),
            Get(values, "departmentId"),
            Get(values, "department"),
            Get(values, "yearLevel", "1"),
            Get(values, "dayOfWeek"),
            Get(values, "startsAt"),
            Get(values, "endsAt"),
            Get(values, "status", "Upcoming")));

    private async Task ApplyAsync(ScheduleEntry entry, Dictionary<string, string> values, CancellationToken ct)
    {
        entry.CourseId = await RelatedIdAsync<Course>(values, "courseId", ct); entry.TeacherId = await RelatedIdAsync<Teacher>(values, "teacherId", ct); entry.ClassroomId = await RelatedIdAsync<Classroom>(values, "classroomId", ct);
        var course = await Db.Courses.FindAsync([entry.CourseId], ct) ?? throw new KeyNotFoundException("Course not found."); var teacher = await Db.Teachers.FindAsync([entry.TeacherId], ct) ?? throw new KeyNotFoundException("Teacher not found."); var room = await Db.Classrooms.FindAsync([entry.ClassroomId], ct) ?? throw new KeyNotFoundException("Classroom not found.");
        var allowCrossDepartment = await SettingEnabledAsync("departments", "allowCrossDepartmentTeaching", false, ct);
        var allowSharedRooms = await SettingEnabledAsync("classrooms", "allowSharedRooms", false, ct);
        if (!course.IsActive || teacher.Status == "Inactive" || room.Status is "Inactive" or "Offline" or "Starting" || (!allowCrossDepartment && teacher.DepartmentId != course.DepartmentId) || (!allowSharedRooms && room.DepartmentId != course.DepartmentId)) throw new InvalidOperationException("Course, teacher, and learning space must comply with Administration department and room-sharing rules.");
        entry.YearLevel = IntInRange(values, "yearLevel", 1, 1, 4);
        entry.DayOfWeek = Enum.TryParse<DayOfWeek>(Required(values, "dayOfWeek"), true, out var day)
            ? day
            : throw new ArgumentException("dayOfWeek is invalid.");
        entry.StartsAt = TimeOnly.TryParse(Required(values, "startsAt"), out var startsAt)
            ? startsAt
            : throw new ArgumentException("startsAt must be a valid time.");
        entry.EndsAt = TimeOnly.TryParse(Required(values, "endsAt"), out var endsAt)
            ? endsAt
            : throw new ArgumentException("endsAt must be a valid time.");
        if (entry.EndsAt <= entry.StartsAt) throw new ArgumentException("Timetable end time must be after start time.");
        if (AcademicTimetablePolicy.Find(entry.DayOfWeek, entry.StartsAt, entry.EndsAt) is null)
            throw new ArgumentException("Select one of the institute's configured teaching periods for this day.");
        if (await Db.ScheduleEntries.AnyAsync(x => x.Id != entry.Id && x.Status != "Cancelled" && x.DayOfWeek == entry.DayOfWeek && x.StartsAt < entry.EndsAt && entry.StartsAt < x.EndsAt && (x.TeacherId == entry.TeacherId || x.ClassroomId == entry.ClassroomId), ct)) throw new InvalidOperationException("Teacher or classroom is already scheduled during this time.");
        entry.Status = OneOf(values, "status", "Upcoming", "Upcoming", "Running", "Completed", "Cancelled");
    }

    private async Task<bool> SettingEnabledAsync(string section, string key, bool fallback, CancellationToken ct) { var value = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == section && x.Key == key).Select(x => x.Value).FirstOrDefaultAsync(ct); return bool.TryParse(value, out var enabled) ? enabled : fallback; }
}
