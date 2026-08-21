using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Students;

public sealed class StudentManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "students";

    public override async Task<IReadOnlyList<CatalogItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct) =>
        (await Db.Students.AsNoTracking().Include(x => x.Department).Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId)).ToListAsync(ct))
        .Where(x => Matches(search, x.FullName, x.StudentNumber, x.Department?.Name))
        .Select(x => Item(x.Id, ("photoDataUrl", x.PhotoDataUrl), ("number", x.StudentNumber), ("name", x.FullName), ("email", x.Email), ("departmentId", x.DepartmentId.ToString()), ("department", x.Department?.Name ?? "—"), ("year", x.YearLevel.ToString()), ("status", x.Status))).ToList();

    public override async Task<CatalogItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = new Student { StudentNumber = Required(values, "number"), FullName = Required(values, "name"), Email = Required(values, "email"), PhotoDataUrl = Required(values, "photoDataUrl"), DepartmentId = await RelatedIdAsync<Department>(values, "departmentId", ct), YearLevel = Int(values, "year", 1), Status = Get(values, "status", "Active") };
        return await SaveCreatedAsync(entity, values, ct);
    }

    public override async Task<CatalogItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Students, id, ct);
        var departmentId = await RelatedIdAsync<Department>(values, "departmentId", ct);
        if (entity.DepartmentId != departmentId && await Db.GradeRecords.AnyAsync(x => x.StudentId == id && x.Course!.DepartmentId != departmentId, ct)) throw new InvalidOperationException("Move or remove this student's grade relationships before changing department.");
        entity.StudentNumber = Required(values, "number"); entity.FullName = Required(values, "name"); entity.Email = Required(values, "email"); entity.PhotoDataUrl = Required(values, "photoDataUrl"); entity.DepartmentId = departmentId; entity.YearLevel = Int(values, "year", 1); entity.Status = Get(values, "status", "Active"); Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }

    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Students.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Student)entity).Status = "Inactive"; Touch(entity); }
}
