using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Courses;

public sealed class CourseManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "courses";
    public override async Task<IReadOnlyList<CatalogItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct) => (await Db.Courses.AsNoTracking().Include(x => x.Department).Include(x => x.Teacher).Where(x => x.IsActive && (!departmentId.HasValue || x.DepartmentId == departmentId)).ToListAsync(ct)).Where(x => Matches(search, x.Code, x.Name, x.Department?.Name)).Select(x => Item(x.Id, ("code", x.Code), ("name", x.Name), ("departmentId", x.DepartmentId.ToString()), ("department", x.Department?.Name ?? "—"), ("teacherId", x.TeacherId?.ToString() ?? ""), ("teacher", x.Teacher?.FullName ?? "Unassigned"), ("credits", x.Credits.ToString()), ("capacity", x.Capacity.ToString()), ("status", "Active"))).ToList();
    public override async Task<CatalogItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var departmentId = await RelatedIdAsync<Department>(values, "departmentId", ct); var teacherId = await RelatedIdAsync<Teacher>(values, "teacherId", ct); await ValidateTeacherAsync(teacherId, departmentId, ct);
        return await SaveCreatedAsync(new Course { Code = Required(values, "code"), Name = Required(values, "name"), DepartmentId = departmentId, TeacherId = teacherId, Credits = Int(values, "credits", 3), Capacity = Int(values, "capacity", 40), IsActive = true }, values, ct);
    }
    public override async Task<CatalogItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Courses, id, ct); var departmentId = await RelatedIdAsync<Department>(values, "departmentId", ct);
        if (entity.DepartmentId != departmentId && (await Db.ScheduleEntries.AnyAsync(x => x.CourseId == id && x.Status != "Cancelled" && (x.Teacher!.DepartmentId != departmentId || x.Classroom!.DepartmentId != departmentId), ct) || await Db.GradeRecords.AnyAsync(x => x.CourseId == id && x.Student!.DepartmentId != departmentId, ct))) throw new InvalidOperationException("Reassign this course's timetable and grade relationships before changing department.");
        if (Get(values, "status", "Active") == "Inactive") await ValidateDeleteAsync(entity, ct);
        var teacherId = await RelatedIdAsync<Teacher>(values, "teacherId", ct); await ValidateTeacherAsync(teacherId, departmentId, ct);
        entity.Code = Required(values, "code"); entity.Name = Required(values, "name"); entity.DepartmentId = departmentId; entity.TeacherId = teacherId; entity.Credits = Int(values, "credits", 3); entity.Capacity = Int(values, "capacity", 40); entity.IsActive = Get(values, "status", "Active") == "Active"; Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct) { if (await Db.ScheduleEntries.AnyAsync(x => x.CourseId == entity.Id && x.Status != "Cancelled", ct) || await Db.GradeRecords.AnyAsync(x => x.CourseId == entity.Id, ct)) throw new InvalidOperationException("Course still has timetable or grade relationships."); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Courses.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Course)entity).IsActive = false; Touch(entity); }
    private async Task ValidateTeacherAsync(Guid teacherId, Guid departmentId, CancellationToken ct) { var teacher = await Db.Teachers.FindAsync([teacherId], ct) ?? throw new KeyNotFoundException("Teacher not found."); if (teacher.Status == "Inactive" || teacher.DepartmentId != departmentId) throw new InvalidOperationException("Course teacher must be active and belong to the same department."); }
}
