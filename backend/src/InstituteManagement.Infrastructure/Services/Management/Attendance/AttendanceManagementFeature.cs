using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Attendance;

public sealed class AttendanceManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "attendance";
    public override async Task<IReadOnlyList<CatalogItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct) => (await Db.AttendanceRecords.AsNoTracking().Include(x => x.Student).ThenInclude(x => x!.Department).Where(x => x.Student!.Status != "Inactive" && (!departmentId.HasValue || x.Student.DepartmentId == departmentId)).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct)).Where(x => Matches(search, x.Student!.FullName, x.Student.StudentNumber, x.Status)).Select(x => Item(x.Id, ("studentId", x.StudentId.ToString()), ("student", x.Student?.FullName ?? "—"), ("number", x.Student?.StudentNumber ?? "—"), ("departmentId", x.Student?.DepartmentId.ToString() ?? ""), ("department", x.Student?.Department?.Name ?? "—"), ("date", x.Date.ToString("yyyy-MM-dd")), ("checkedInAt", x.CheckedInAt?.ToString("HH:mm") ?? ""), ("status", x.Status), ("method", x.Method))).ToList();
    public override async Task<CatalogItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct) => await SaveCreatedAsync(await BuildAsync(new AttendanceRecord(), values, ct), values, ct);
    public override async Task<CatalogItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct) { var entity = await RequiredEntityAsync(Db.AttendanceRecords, id, ct); await BuildAsync(entity, values, ct); Touch(entity); return await SaveUpdatedAsync(id, values, ct); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.AttendanceRecords.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) => Db.Remove(entity);
    private async Task<AttendanceRecord> BuildAsync(AttendanceRecord entity, Dictionary<string, string> values, CancellationToken ct) { entity.StudentId = await RelatedIdAsync<Student>(values, "studentId", ct); entity.Date = DateOnly.Parse(Required(values, "date")); entity.CheckedInAt = TimeOnly.TryParse(Get(values, "checkedInAt"), out var time) ? time : null; entity.Status = Get(values, "status", "Present"); entity.Method = Get(values, "method", "ID Card"); return entity; }
}
