using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Departments;

public sealed class DepartmentManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "departments";
    public override async Task<IReadOnlyList<CatalogItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct) => (await Db.Departments.AsNoTracking().Include(x => x.HeadTeacher).Where(x => x.IsActive && (!departmentId.HasValue || x.Id == departmentId)).ToListAsync(ct)).Where(x => Matches(search, x.Code, x.Name, x.HeadTeacher?.FullName, x.Head)).Select(x => Item(x.Id, ("code", x.Code), ("name", x.Name), ("headTeacherId", x.HeadTeacherId?.ToString() ?? ""), ("head", x.HeadTeacher?.FullName ?? x.Head), ("status", "Active"))).ToList();
    public override async Task<CatalogItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var headId = await RelatedIdAsync<Teacher>(values, "headTeacherId", ct); var teacher = await Db.Teachers.FindAsync([headId], ct) ?? throw new KeyNotFoundException("Head teacher not found.");
        var department = new Department { Code = Required(values, "code"), Name = Required(values, "name"), HeadTeacherId = headId, Head = teacher.FullName, IsActive = true };
        Db.Departments.Add(department); Db.AuditLogs.Add(Audit(department.Id, values, "Created")); await Db.SaveChangesAsync(ct); teacher.DepartmentId = department.Id; await Db.SaveChangesAsync(ct); await Cache.InvalidateDashboardAsync(); return Item(department.Id, values);
    }
    public override async Task<CatalogItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Departments, id, ct); if (Get(values, "status", "Active") == "Inactive") await ValidateDeleteAsync(entity, ct);
        var headId = await RelatedIdAsync<Teacher>(values, "headTeacherId", ct); var head = await Db.Teachers.FindAsync([headId], ct) ?? throw new KeyNotFoundException("Head teacher not found.");
        if (head.DepartmentId != id && (await Db.Courses.AnyAsync(x => x.TeacherId == head.Id && x.DepartmentId != id, ct) || await Db.ScheduleEntries.AnyAsync(x => x.TeacherId == head.Id && x.Course!.DepartmentId != id, ct))) throw new InvalidOperationException("Move the selected head teacher's course and timetable relationships first.");
        entity.Code = Required(values, "code"); entity.Name = Required(values, "name"); entity.HeadTeacherId = headId; entity.Head = head.FullName; entity.IsActive = Get(values, "status", "Active") == "Active"; head.DepartmentId = id; Touch(entity); return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct) { var id = entity.Id; if (await Db.Students.AnyAsync(x => x.DepartmentId == id && x.Status != "Inactive", ct) || await Db.Teachers.AnyAsync(x => x.DepartmentId == id && x.Status != "Inactive", ct) || await Db.Courses.AnyAsync(x => x.DepartmentId == id && x.IsActive, ct) || await Db.Classrooms.AnyAsync(x => x.DepartmentId == id && x.Status != "Inactive", ct)) throw new InvalidOperationException("Department still contains active students, teachers, courses, or classrooms."); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Departments.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Department)entity).IsActive = false; Touch(entity); }
}
