using InstituteManagement.Application.DTOs.Management;
using InstituteManagement.Application.DTOs.Management.Courses;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Courses;

public sealed class CourseManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "courses";
    public override async Task<IReadOnlyList<IManagementItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var courses = await Db.Courses.AsNoTracking().Include(course => course.Department).Include(course => course.Teacher)
            .Where(course => course.IsActive && (!departmentId.HasValue || course.DepartmentId == departmentId))
            .ToListAsync(ct);
        return courses.Where(course => Matches(search, course.CourseCode, course.Name, course.Department?.Name))
            .Select(course => (IManagementItemDto)new CourseResponseDto(course.Id, new CourseValuesDto(
                course.CourseCode,
                course.Name,
                course.DepartmentId.ToString(),
                course.Department?.Name ?? "—",
                course.TeacherId?.ToString() ?? "",
                course.Teacher?.FullName ?? "Unassigned",
                course.Capacity.ToString(),
                "Active",
                course.CreateAt.ToString("yyyy-MM-dd"))))
            .ToList();
    }

    public override async Task<IManagementItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var courseCode = Required(values, "courseCode");
        await EnsureUniqueAsync(Db.Courses.Where(course => course.CourseCode == courseCode), "CourseCode", ct);
        var departmentId = await RelatedIdAsync<Department>(values, "departmentId", ct);
        var teacherId = await TeacherIdAsync(values, departmentId, ct);
        return await SaveCreatedAsync(new Course
        {
            CourseCode = courseCode,
            Name = Required(values, "name"),
            DepartmentId = departmentId,
            TeacherId = teacherId,
            Capacity = IntInRange(values, "capacity", 40, 1, 10000),
            IsActive = CourseStatus(values) == "Active"
        }, values, ct);
    }
    public override async Task<IManagementItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Courses, id, ct);
        var courseCode = Required(values, "courseCode");
        await EnsureUniqueAsync(Db.Courses.Where(course => course.Id != id && course.CourseCode == courseCode), "CourseCode", ct);
        var departmentId = await RelatedIdAsync<Department>(values, "departmentId", ct);
        if (entity.DepartmentId != departmentId && (await Db.ScheduleEntries.AnyAsync(x => x.CourseId == id && x.Status != "Cancelled" && (x.Teacher!.DepartmentId != departmentId || x.Classroom!.DepartmentId != departmentId), ct) || await Db.GradeRecords.AnyAsync(x => x.CourseId == id && x.Student!.DepartmentId != departmentId, ct))) throw new InvalidOperationException("Reassign this course's timetable and grade relationships before changing department.");
        var status = CourseStatus(values);
        if (status == "Inactive") await ValidateDeleteAsync(entity, ct);
        var teacherId = await TeacherIdAsync(values, departmentId, ct);
        entity.CourseCode = courseCode;
        entity.Name = Required(values, "name");
        entity.DepartmentId = departmentId;
        entity.TeacherId = teacherId;
        entity.Capacity = IntInRange(values, "capacity", 40, 1, 10000);
        entity.IsActive = status == "Active";
        Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct) { if (await Db.ScheduleEntries.AnyAsync(x => x.CourseId == entity.Id && x.Status != "Cancelled", ct) || await Db.GradeRecords.AnyAsync(x => x.CourseId == entity.Id, ct)) throw new InvalidOperationException("Course still has timetable or grade relationships."); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Courses.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Course)entity).IsActive = false; Touch(entity); }
    protected override IManagementItemDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new CourseResponseDto(id, new CourseValuesDto(
            Get(values, "courseCode"),
            Get(values, "name"),
            Get(values, "departmentId"),
            Get(values, "department"),
            Get(values, "teacherId"),
            Get(values, "teacher", "Unassigned"),
            Get(values, "capacity"),
            Get(values, "status", "Active"),
            Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

    private async Task<Guid?> TeacherIdAsync(Dictionary<string, string> values, Guid departmentId, CancellationToken ct)
    {
        var raw = Get(values, "teacherId");
        var required = await SettingEnabledAsync("courses", "requireAssignedTeacher", true, ct);
        if (string.IsNullOrWhiteSpace(raw)) { if (required) throw new ArgumentException("Assigned teacher is required by Course settings."); return null; }
        if (!Guid.TryParse(raw, out var teacherId)) throw new ArgumentException("teacherId is invalid.");
        await ValidateTeacherAsync(teacherId, departmentId, ct);
        return teacherId;
    }
    private async Task ValidateTeacherAsync(Guid? teacherId, Guid departmentId, CancellationToken ct) { if (!teacherId.HasValue) return; var teacher = await Db.Teachers.FindAsync([teacherId.Value], ct) ?? throw new KeyNotFoundException("Teacher not found."); var allowCrossDepartment = await SettingEnabledAsync("departments", "allowCrossDepartmentTeaching", false, ct); if (teacher.Status == "Inactive" || (!allowCrossDepartment && teacher.DepartmentId.HasValue && teacher.DepartmentId != departmentId)) throw new InvalidOperationException("Course teacher must be active and comply with Department settings."); teacher.DepartmentId ??= departmentId; }
    private async Task<bool> SettingEnabledAsync(string section, string key, bool fallback, CancellationToken ct) { var value = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == section && x.Key == key).Select(x => x.Value).FirstOrDefaultAsync(ct); return bool.TryParse(value, out var enabled) ? enabled : fallback; }
    private static string CourseStatus(Dictionary<string, string> values) =>
        OneOf(values, "status", "Active", "Active", "Inactive");
}
