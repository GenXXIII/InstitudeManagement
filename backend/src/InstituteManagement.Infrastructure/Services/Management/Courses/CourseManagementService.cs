using InstituteManagement.Application.Features.Management.Courses;
using InstituteManagement.Infrastructure.Services.Catalog;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Courses;

public sealed class CourseManagementService(InstituteDbContext db, InstituteCache cache) : CatalogFeatureBase<CourseResponseDto>(db, cache), ICourseManagementService
{
    public override CatalogResource Resource => CatalogResource.Courses;
    public override async Task<IReadOnlyList<CourseResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var courses = await Db.Courses.AsNoTracking().Where(course => course.IsActive && (!departmentId.HasValue || course.DepartmentId == departmentId)).ToListAsync(ct);
        return courses.Where(course => Matches(search, course.CourseCode, course.Name)).Select(course => new CourseResponseDto(course.Id, new CourseValuesDto(course.CourseCode, course.Name, "", "", "", "", "", "Active", course.CreateAt.ToString("yyyy-MM-dd")))).ToList();
    }
    public override async Task<CourseResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var code = RequiredCode(values, "courseCode");
        await EnsureUniqueAsync(Db.Courses.Where(course => course.CourseCode == code), "CourseCode", ct);
        return await SaveCreatedAsync(new Course { CourseCode = code, Name = Required(values, "name"), DepartmentId = null, TeacherId = null, Capacity = 0, IsActive = true }, values, ct);
    }
    public override async Task<CourseResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Courses, id, ct); var code = RequiredCode(values, "courseCode"); await EnsureUniqueAsync(Db.Courses.Where(course => course.Id != id && course.CourseCode == code), "CourseCode", ct);
        entity.CourseCode = code; entity.Name = Required(values, "name"); Touch(entity); return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct) { if (await Db.CourseAssignments.AnyAsync(x => x.CourseId == entity.Id && x.Status == "Active", ct) || await Db.ScheduleEntries.AnyAsync(x => x.CourseId == entity.Id && x.Status != "Cancelled", ct) || await Db.GradeRecords.AnyAsync(x => x.CourseId == entity.Id, ct)) throw new InvalidOperationException("Course still has enrollment, timetable, or grade relationships."); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Courses.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Course)entity).IsActive = false; Touch(entity); }
    protected override CourseResponseDto Response(Guid id, IReadOnlyDictionary<string, string> values) => new CourseResponseDto(id, new CourseValuesDto(Get(values, "courseCode"), Get(values, "name"), "", "", "", "", "", Get(values, "status", "Active"), Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
}
