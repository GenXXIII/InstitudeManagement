using InstituteManagement.Application.Features.Management.Students;
using InstituteManagement.Infrastructure.Services.Catalog;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Students;

public sealed class StudentManagementService(InstituteDbContext db, InstituteCache cache) : CatalogFeatureBase<StudentResponseDto>(db, cache), IStudentManagementService
{
    public override CatalogResource Resource => CatalogResource.Students;
    public override async Task<IReadOnlyList<StudentResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var students = await Db.Students.AsNoTracking().Where(student => student.Status != "Inactive" && (!departmentId.HasValue || student.DepartmentId == departmentId)).ToListAsync(ct);
        return students.Where(student => Matches(search, student.FullName, student.StudentCode, student.Email))
            .Select(student => new StudentResponseDto(student.Id, new StudentValuesDto(student.PhotoDataUrl, student.StudentCode, student.FullName, student.Email, "", "", "", "", student.Status, student.CreateAt.ToString("yyyy-MM-dd")))).ToList();
    }
    public override async Task<StudentResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var code = RequiredCode(values, "studentCode");
        await EnsureUniqueAsync(Db.Students.Where(student => student.StudentCode == code), "StudentCode", ct);
        return await SaveCreatedAsync(new Student { StudentCode = code, FullName = Required(values, "name"), Email = Email(values, "email"), PhotoDataUrl = Required(values, "photoDataUrl"), DepartmentId = null, YearLevel = 0, Shift = "", Status = "Active" }, values, ct);
    }
    public override async Task<StudentResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Students, id, ct); var code = RequiredCode(values, "studentCode");
        await EnsureUniqueAsync(Db.Students.Where(student => student.Id != id && student.StudentCode == code), "StudentCode", ct);
        entity.StudentCode = code; entity.FullName = Required(values, "name"); entity.Email = Email(values, "email"); entity.PhotoDataUrl = Required(values, "photoDataUrl"); Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Students.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Student)entity).Status = "Inactive"; Touch(entity); }
    protected override StudentResponseDto Response(Guid id, IReadOnlyDictionary<string, string> values) => new StudentResponseDto(id, new StudentValuesDto(Get(values, "photoDataUrl"), Get(values, "studentCode"), Get(values, "name"), Get(values, "email"), "", "", "", "", Get(values, "status", "Active"), Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
}
