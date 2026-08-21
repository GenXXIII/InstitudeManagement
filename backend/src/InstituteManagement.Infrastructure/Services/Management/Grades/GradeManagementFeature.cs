using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Grades;

public sealed class GradeManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "grades";
    public override async Task<IReadOnlyList<CatalogItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct) => (await Db.GradeRecords.AsNoTracking().Include(x => x.Student).ThenInclude(x => x!.Department).Include(x => x.Course).Where(x => x.Student!.Status != "Inactive" && x.Course!.IsActive && (!departmentId.HasValue || x.Student.DepartmentId == departmentId)).ToListAsync(ct)).Where(x => Matches(search, x.Student!.FullName, x.Course!.Name, x.LetterGrade)).Select(x => Item(x.Id, ("studentId", x.StudentId.ToString()), ("student", x.Student?.FullName ?? "—"), ("courseId", x.CourseId.ToString()), ("course", x.Course?.Name ?? "—"), ("departmentId", x.Student?.DepartmentId.ToString() ?? ""), ("department", x.Student?.Department?.Name ?? "—"), ("score", x.Score.ToString("0.0")), ("grade", x.LetterGrade), ("term", x.Term))).ToList();
    public override async Task<CatalogItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct) => await SaveCreatedAsync(await BuildAsync(new GradeRecord(), values, ct), values, ct);
    public override async Task<CatalogItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct) { var entity = await RequiredEntityAsync(Db.GradeRecords, id, ct); await BuildAsync(entity, values, ct); Touch(entity); return await SaveUpdatedAsync(id, values, ct); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.GradeRecords.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) => Db.Remove(entity);
    private async Task<GradeRecord> BuildAsync(GradeRecord entity, Dictionary<string, string> values, CancellationToken ct)
    {
        entity.StudentId = await RelatedIdAsync<Student>(values, "studentId", ct); entity.CourseId = await RelatedIdAsync<Course>(values, "courseId", ct); var student = await Db.Students.FindAsync([entity.StudentId], ct); var course = await Db.Courses.FindAsync([entity.CourseId], ct); if (student?.DepartmentId != course?.DepartmentId || student?.Status == "Inactive" || course?.IsActive != true) throw new InvalidOperationException("Student and course must be active and belong to the same department."); entity.Score = Decimal(values, "score"); entity.LetterGrade = await LetterAsync(entity.Score, ct); entity.Term = Get(values, "term", "Semester 1"); return entity;
    }
    private async Task<string> LetterAsync(decimal score, CancellationToken ct) { var settings = await Db.SystemSettings.Where(x => x.Section == "grade-rules").ToDictionaryAsync(x => x.Key, x => x.Value, ct); var a = decimal.TryParse(settings.GetValueOrDefault("aMinimum"), out var av) ? av : 90; var b = decimal.TryParse(settings.GetValueOrDefault("bMinimum"), out var bv) ? bv : 80; var c = decimal.TryParse(settings.GetValueOrDefault("cMinimum"), out var cv) ? cv : 70; var d = decimal.TryParse(settings.GetValueOrDefault("dMinimum"), out var dv) ? dv : 60; return score >= a ? "A" : score >= b ? "B" : score >= c ? "C" : score >= d ? "D" : "F"; }
}
