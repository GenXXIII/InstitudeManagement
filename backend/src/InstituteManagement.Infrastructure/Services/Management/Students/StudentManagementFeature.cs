using InstituteManagement.Application.DTOs.Management;
using InstituteManagement.Application.DTOs.Management.Students;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Students;

public sealed class StudentManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "students";

    public override async Task<IReadOnlyList<IManagementItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var students = await Db.Students
            .AsNoTracking()
            .Include(student => student.Department)
            .Where(student => student.Status != "Inactive" && (!departmentId.HasValue || student.DepartmentId == departmentId))
            .ToListAsync(ct);

        return students
            .Where(student => Matches(search, student.FullName, student.StudentNumber, student.Department?.Name))
            .Select(student => (IManagementItemDto)new StudentResponseDto(
                student.Id,
                new StudentValuesDto(
                    student.PhotoDataUrl,
                    student.StudentNumber,
                    student.FullName,
                    student.Email,
                    student.DepartmentId.ToString(),
                    student.Department?.Name ?? "—",
                    student.YearLevel.ToString(),
                    student.Status)))
            .ToList();
    }

    public override async Task<IManagementItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var number = Required(values, "number");
        await EnsureUniqueAsync(Db.Students.Where(student => student.StudentNumber == number), "Student ID", ct);
        var entity = new Student
        {
            StudentNumber = number,
            FullName = Required(values, "name"),
            Email = Email(values, "email"),
            PhotoDataUrl = Required(values, "photoDataUrl"),
            DepartmentId = await RelatedIdAsync<Department>(values, "departmentId", ct),
            YearLevel = IntInRange(values, "year", 1, 1, 12),
            Status = OneOf(values, "status", "Active", "Active", "Inactive")
        };
        return await SaveCreatedAsync(entity, values, ct);
    }

    public override async Task<IManagementItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Students, id, ct);
        var number = Required(values, "number");
        await EnsureUniqueAsync(Db.Students.Where(student => student.Id != id && student.StudentNumber == number), "Student ID", ct);
        var departmentId = await RelatedIdAsync<Department>(values, "departmentId", ct);
        if (entity.DepartmentId != departmentId && await Db.GradeRecords.AnyAsync(x => x.StudentId == id && x.Course!.DepartmentId != departmentId, ct)) throw new InvalidOperationException("Move or remove this student's grade relationships before changing department.");
        entity.StudentNumber = number;
        entity.FullName = Required(values, "name");
        entity.Email = Email(values, "email");
        entity.PhotoDataUrl = Required(values, "photoDataUrl");
        entity.DepartmentId = departmentId;
        entity.YearLevel = IntInRange(values, "year", 1, 1, 12);
        entity.Status = OneOf(values, "status", "Active", "Active", "Inactive");
        Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }

    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Students.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Student)entity).Status = "Inactive"; Touch(entity); }
    protected override IManagementItemDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new StudentResponseDto(id, new StudentValuesDto(
            Get(values, "photoDataUrl"),
            Get(values, "number"),
            Get(values, "name"),
            Get(values, "email"),
            Get(values, "departmentId"),
            Get(values, "department"),
            Get(values, "year"),
            Get(values, "status", "Active")));
}
