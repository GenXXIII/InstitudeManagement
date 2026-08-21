using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Classrooms;

public sealed class ClassroomManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "classrooms";
    public override async Task<IReadOnlyList<CatalogItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct) => (await Db.Classrooms.AsNoTracking().Include(x => x.Department).Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId)).ToListAsync(ct)).Where(x => Matches(search, x.Code, x.Building, x.Status, x.Department?.Name)).Select(x => Item(x.Id, ("code", x.Code), ("building", x.Building), ("departmentId", x.DepartmentId?.ToString() ?? ""), ("department", x.Department?.Name ?? "Shared"), ("capacity", x.Capacity.ToString()), ("status", x.Status), ("deviceOnline", x.DeviceOnline.ToString().ToLowerInvariant()))).ToList();
    public override async Task<CatalogItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct) => await SaveCreatedAsync(new Classroom { Code = Required(values, "code"), Building = Required(values, "building"), DepartmentId = await RelatedIdAsync<Department>(values, "departmentId", ct), Capacity = Int(values, "capacity", 40), Status = Get(values, "status", "Available"), DeviceOnline = Bool(values, "deviceOnline", true) }, values, ct);
    public override async Task<CatalogItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Classrooms, id, ct); var departmentId = await RelatedIdAsync<Department>(values, "departmentId", ct);
        if (entity.DepartmentId != departmentId && await Db.ScheduleEntries.AnyAsync(x => x.ClassroomId == id && x.Course!.DepartmentId != departmentId && x.Status != "Cancelled", ct)) throw new InvalidOperationException("Move this classroom's active timetable entries before changing department.");
        if (Get(values, "status", "Available") == "Inactive") await ValidateDeleteAsync(entity, ct);
        entity.Code = Required(values, "code"); entity.Building = Required(values, "building"); entity.DepartmentId = departmentId; entity.Capacity = Int(values, "capacity", 40); entity.Status = Get(values, "status", "Available"); entity.DeviceOnline = Bool(values, "deviceOnline", true); Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct) { if (await Db.ScheduleEntries.AnyAsync(x => x.ClassroomId == entity.Id && x.Status != "Cancelled", ct)) throw new InvalidOperationException("Classroom is still used by an active timetable entry."); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Classrooms.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { var room = (Classroom)entity; room.Status = "Inactive"; room.DeviceOnline = false; Touch(room); }
}
