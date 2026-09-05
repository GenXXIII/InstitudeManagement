using InstituteManagement.Application.Features.Management.Departments;
using InstituteManagement.Infrastructure.Services.Catalog;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Departments;

public sealed class DepartmentManagementService(InstituteDbContext db, InstituteCache cache) : CatalogFeatureBase<DepartmentResponseDto>(db, cache), IDepartmentManagementService
{
    public override CatalogResource Resource => CatalogResource.Departments;
    public override async Task<IReadOnlyList<DepartmentResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var departments = await Db.Departments.AsNoTracking().Include(department => department.HeadTeacher)
            .Where(department => department.IsActive && (!departmentId.HasValue || department.Id == departmentId))
            .ToListAsync(ct);
        return departments.Where(department => Matches(search, department.DepartmentCode, department.Name, department.HeadTeacher?.FullName, department.Head))
            .Select(department => new DepartmentResponseDto(department.Id, new DepartmentValuesDto(
                department.DepartmentCode,
                department.Name,
                department.HeadTeacherId?.ToString() ?? "",
                department.HeadTeacher?.FullName ?? department.Head,
                "Active",
                department.CreateAt.ToString("yyyy-MM-dd"))))
            .ToList();
    }

    public override async Task<DepartmentResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var departmentCode = await ConfiguredCodeAsync(values, "departmentCode", "department", ct); values["departmentCode"] = departmentCode;
        await EnsureUniqueAsync(Db.Departments.Where(department => department.DepartmentCode == departmentCode), "DepartmentCode", ct);
        var (headId, teacher) = await HeadAsync(values, ct);
        var status = await DepartmentStatusAsync(values, null, ct);
        var department = new Department { DepartmentCode = departmentCode, Name = Required(values, "name"), HeadTeacherId = headId, Head = teacher?.FullName ?? "Not appointed", IsActive = status == "Active" };
        Db.Departments.Add(department); Db.AuditLogs.Add(Audit(department.Id, values, "Created")); await Db.SaveChangesAsync(ct); if (teacher is not null) { teacher.DepartmentId = department.Id; await Db.SaveChangesAsync(ct); }
        await Cache.InvalidateDashboardAsync(ct); return Response(department.Id, values);
    }
    public override async Task<DepartmentResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Departments, id, ct);
        var departmentCode = await ConfiguredCodeAsync(values, "departmentCode", "department", ct); values["departmentCode"] = departmentCode;
        await EnsureUniqueAsync(Db.Departments.Where(department => department.Id != id && department.DepartmentCode == departmentCode), "DepartmentCode", ct);
        var status = await DepartmentStatusAsync(values, entity.IsActive ? "Active" : "Inactive", ct);
        if (status == "Inactive") await ValidateDeleteAsync(entity, ct);
        var (headId, head) = await HeadAsync(values, ct);
        if (head is not null && head.DepartmentId != id && (await Db.Courses.AnyAsync(x => x.TeacherId == head.Id && x.DepartmentId != id, ct) || await Db.ScheduleEntries.AnyAsync(x => x.TeacherId == head.Id && x.Course!.DepartmentId != id, ct))) throw new InvalidOperationException("Move the selected head teacher's course and timetable relationships first.");
        entity.DepartmentCode = departmentCode;
        entity.Name = Required(values, "name");
        entity.HeadTeacherId = headId;
        entity.Head = head?.FullName ?? "Not appointed";
        entity.IsActive = status == "Active";
        if (head is not null) head.DepartmentId = id;
        Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct) { var id = entity.Id; if (await Db.Students.AnyAsync(x => x.DepartmentId == id && x.Status != "Inactive", ct) || await Db.Teachers.AnyAsync(x => x.DepartmentId == id && x.Status != "Inactive", ct) || await Db.Courses.AnyAsync(x => x.DepartmentId == id && x.IsActive, ct)) throw new InvalidOperationException("Department still contains active students, teachers, or courses."); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Departments.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Department)entity).IsActive = false; Touch(entity); }
    protected override DepartmentResponseDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new DepartmentResponseDto(id, new DepartmentValuesDto(
            Get(values, "departmentCode"),
            Get(values, "name"),
            Get(values, "headTeacherId"),
            Get(values, "head"),
            Get(values, "status", "Active"),
            Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

    private async Task<string> DepartmentStatusAsync(Dictionary<string, string> values, string? fallback, CancellationToken ct)
    {
        var configured = fallback ?? await Db.SystemSettings.AsNoTracking()
            .Where(x => x.Section == "departments" && x.Key == "defaultStatus")
            .Select(x => x.Value)
            .FirstOrDefaultAsync(ct) ?? "Active";
        return OneOf(values, "status", configured, "Active", "Inactive");
    }

    private async Task<(Guid? Id, Teacher? Teacher)> HeadAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var raw = Get(values, "headTeacherId");
        var setting = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == "departments" && x.Key == "requireDepartmentHead").Select(x => x.Value).FirstOrDefaultAsync(ct);
        var required = !bool.TryParse(setting, out var enabled) || enabled;
        if (string.IsNullOrWhiteSpace(raw)) { if (required) throw new ArgumentException("Head of department is required by Department settings."); return (null, null); }
        if (!Guid.TryParse(raw, out var id)) throw new ArgumentException("headTeacherId is invalid.");
        return (id, await Db.Teachers.FindAsync([id], ct) ?? throw new KeyNotFoundException("Head teacher not found."));
    }
}
