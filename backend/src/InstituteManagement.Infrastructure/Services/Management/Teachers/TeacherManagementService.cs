using InstituteManagement.Application.Features.Management.Teachers;
using InstituteManagement.Infrastructure.Services.Catalog;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Teachers;

public sealed class TeacherManagementService(InstituteDbContext db, InstituteCache cache) : CatalogFeatureBase<TeacherResponseDto>(db, cache), ITeacherManagementService
{
    public override CatalogResource Resource => CatalogResource.Teachers;
    public override async Task<IReadOnlyList<TeacherResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var teachers = await Db.Teachers.AsNoTracking().Where(teacher => teacher.Status != "Inactive" && (!departmentId.HasValue || teacher.DepartmentId == departmentId)).ToListAsync(ct);
        return teachers.Where(teacher => Matches(search, teacher.FullName, teacher.TeacherCode, teacher.Email)).Select(teacher => new TeacherResponseDto(teacher.Id, new TeacherValuesDto(teacher.PhotoDataUrl, teacher.TeacherCode, teacher.FullName, teacher.Email, "", "", teacher.Status, teacher.CreateAt.ToString("yyyy-MM-dd")))).ToList();
    }
    public override async Task<TeacherResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var code = RequiredCode(values, "teacherCode");
        await EnsureUniqueAsync(Db.Teachers.Where(teacher => teacher.TeacherCode == code), "TeacherCode", ct);
        return await SaveCreatedAsync(new Teacher { TeacherCode = code, FullName = Required(values, "name"), Email = Email(values, "email"), PhotoDataUrl = Required(values, "photoDataUrl"), DepartmentId = null, Status = "Available" }, values, ct);
    }
    public override async Task<TeacherResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Teachers, id, ct); var code = RequiredCode(values, "teacherCode"); await EnsureUniqueAsync(Db.Teachers.Where(teacher => teacher.Id != id && teacher.TeacherCode == code), "TeacherCode", ct);
        entity.TeacherCode = code; entity.FullName = Required(values, "name"); entity.Email = Email(values, "email"); entity.PhotoDataUrl = Required(values, "photoDataUrl"); Touch(entity); return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct) { var id = entity.Id; if (await Db.Departments.AnyAsync(x => x.HeadTeacherId == id, ct) || await Db.CourseAssignments.AnyAsync(x => x.TeacherId == id && x.Status == "Active", ct) || await Db.ScheduleEntries.AnyAsync(x => x.TeacherId == id && x.Status != "Cancelled", ct)) throw new InvalidOperationException("Teacher is still assigned as a department head, course teacher, or timetable teacher."); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Teachers.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Teacher)entity).Status = "Inactive"; Touch(entity); }
    protected override TeacherResponseDto Response(Guid id, IReadOnlyDictionary<string, string> values) => new TeacherResponseDto(id, new TeacherValuesDto(Get(values, "photoDataUrl"), Get(values, "teacherCode"), Get(values, "name"), Get(values, "email"), "", "", Get(values, "status", "Available"), Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
}
