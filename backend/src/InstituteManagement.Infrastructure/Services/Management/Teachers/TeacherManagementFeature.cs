using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Teachers;

public sealed class TeacherManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "teachers";
    public override async Task<IReadOnlyList<CatalogItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct) =>
        (await Db.Teachers.AsNoTracking().Include(x => x.Department).Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId)).ToListAsync(ct)).Where(x => Matches(search, x.FullName, x.TeacherNumber, x.Department?.Name)).Select(x => Item(x.Id, ("photoDataUrl", x.PhotoDataUrl), ("number", x.TeacherNumber), ("name", x.FullName), ("email", x.Email), ("departmentId", x.DepartmentId.ToString()), ("department", x.Department?.Name ?? "—"), ("status", x.Status))).ToList();

    public override async Task<CatalogItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct) => await SaveCreatedAsync(new Teacher { TeacherNumber = Required(values, "number"), FullName = Required(values, "name"), Email = Required(values, "email"), PhotoDataUrl = Required(values, "photoDataUrl"), DepartmentId = await RelatedIdAsync<Department>(values, "departmentId", ct), Status = Get(values, "status", "Available") }, values, ct);

    public override async Task<CatalogItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Teachers, id, ct); var departmentId = await RelatedIdAsync<Department>(values, "departmentId", ct);
        if (entity.DepartmentId != departmentId && (await Db.Departments.AnyAsync(x => x.HeadTeacherId == id && x.Id != departmentId, ct) || await Db.Courses.AnyAsync(x => x.TeacherId == id && x.DepartmentId != departmentId, ct) || await Db.ScheduleEntries.AnyAsync(x => x.TeacherId == id && x.Course!.DepartmentId != departmentId, ct))) throw new InvalidOperationException("Reassign this teacher's department-head, course, and timetable relationships first.");
        if (Get(values, "status", "Available") == "Inactive") await ValidateDeleteAsync(entity, ct);
        entity.TeacherNumber = Required(values, "number"); entity.FullName = Required(values, "name"); entity.Email = Required(values, "email"); entity.PhotoDataUrl = Required(values, "photoDataUrl"); entity.DepartmentId = departmentId; entity.Status = Get(values, "status", "Available"); Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }

    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct)
    {
        var id = entity.Id;
        if (await Db.Departments.AnyAsync(x => x.HeadTeacherId == id, ct) || await Db.Courses.AnyAsync(x => x.TeacherId == id && x.IsActive, ct) || await Db.ScheduleEntries.AnyAsync(x => x.TeacherId == id && x.Status != "Cancelled", ct)) throw new InvalidOperationException("Teacher is still assigned as a department head, course teacher, or timetable teacher.");
    }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Teachers.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Teacher)entity).Status = "Inactive"; Touch(entity); }
}
