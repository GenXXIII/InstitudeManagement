using InstituteManagement.Application.Common.Pagination;
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
    public async Task<PagedResult<StudentResponseDto>> GetAsync(string? search, Guid? departmentId, Guid? profileId, PageRequest page, CancellationToken ct)
    {
        var query = Db.Students.AsNoTracking()
            .Where(student => student.Status != "Inactive" && (!departmentId.HasValue || student.DepartmentId == departmentId) && (!profileId.HasValue || student.Id == profileId));

        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(student =>
                student.FullName.Contains(term)
                || student.StudentCode.Contains(term)
                || student.Email.Contains(term));

        var totalCount = await query.CountAsync(ct);

        var students = await query
            .OrderBy(student => student.StudentCode)
            .ThenBy(student => student.Id)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(student => new
            {
                student.Id,
                student.PhotoDataUrl,
                student.StudentCode,
                student.FullName,
                student.Email,
                student.Status,
                student.CreateAt
            })
            .ToListAsync(ct);

        var items = students.Select(student => new StudentResponseDto(
            student.Id,
            new StudentValuesDto(
                student.PhotoDataUrl,
                student.StudentCode,
                "",
                student.FullName,
                student.Email,
                "",
                "",
                "",
                "",
                student.Status,
                student.CreateAt.ToString("yyyy-MM-dd"))))
            .ToList();
        return PagedResult<StudentResponseDto>.Create(items, page, totalCount);
    }
    public override async Task<StudentResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var code = await ConfiguredCodeAsync(values, "studentCode", "student", ct); values["studentCode"] = code;
        await EnsureUniqueCodeAsync(Db.Students.Select(student => student.StudentCode), code, "StudentCode", ct);
        var entity = new Student { StudentCode = code, PublicId = string.Empty, FullName = Required(values, "name"), Email = Email(values, "email"), PhotoDataUrl = Required(values, "photoDataUrl"), DepartmentId = null, YearLevel = 0, Shift = "", Status = "Active" };
        values["publicId"] = string.Empty;
        return await SaveCreatedAsync(entity, values, ct);
    }
    public override async Task<StudentResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Students, id, ct); values["studentCode"] = entity.StudentCode; values["publicId"] = string.Empty;
        entity.FullName = Required(values, "name"); entity.Email = Email(values, "email"); entity.PhotoDataUrl = Required(values, "photoDataUrl"); Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Students.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Student)entity).Status = "Inactive"; Touch(entity); }
    protected override StudentResponseDto Response(Guid id, IReadOnlyDictionary<string, string> values) => new StudentResponseDto(id, new StudentValuesDto(Get(values, "photoDataUrl"), Get(values, "studentCode"), Get(values, "publicId"), Get(values, "name"), Get(values, "email"), "", "", "", "", Get(values, "status", "Active"), Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
}
