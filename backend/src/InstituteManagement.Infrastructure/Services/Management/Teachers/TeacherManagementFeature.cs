using InstituteManagement.Application.DTOs.Management;
using InstituteManagement.Application.DTOs.Management.Teachers;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Teachers;

public sealed class TeacherManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "teachers";
    public override async Task<IReadOnlyList<IManagementItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var teachers = await Db.Teachers
            .AsNoTracking()
            .Include(teacher => teacher.Department)
            .Where(teacher => teacher.Status != "Inactive" && (!departmentId.HasValue || teacher.DepartmentId == departmentId))
            .ToListAsync(ct);

        return teachers
            .Where(teacher => Matches(search, teacher.FullName, teacher.TeacherCode, teacher.Department?.Name))
            .Select(teacher => (IManagementItemDto)new TeacherResponseDto(
                teacher.Id,
                new TeacherValuesDto(
                    teacher.PhotoDataUrl,
                    teacher.TeacherCode,
                    teacher.FullName,
                    teacher.Email,
                    teacher.DepartmentId?.ToString() ?? "",
                    teacher.Department?.Name ?? "—",
                    teacher.Status,
                    teacher.CreateAt.ToString("yyyy-MM-dd"))))
            .ToList();
    }

    public override async Task<IManagementItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var teacherCode = Required(values, "teacherCode");
        await EnsureUniqueAsync(Db.Teachers.Where(teacher => teacher.TeacherCode == teacherCode), "TeacherCode", ct);
        return await SaveCreatedAsync(new Teacher
        {
            TeacherCode = teacherCode,
            FullName = Required(values, "name"),
            Email = Email(values, "email"),
            PhotoDataUrl = Required(values, "photoDataUrl"),
            Status = TeacherStatus(values)
        }, values, ct);
    }

    public override async Task<IManagementItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Teachers, id, ct);
        var teacherCode = Required(values, "teacherCode");
        await EnsureUniqueAsync(Db.Teachers.Where(teacher => teacher.Id != id && teacher.TeacherCode == teacherCode), "TeacherCode", ct);
        var status = TeacherStatus(values);
        if (status == "Inactive") await ValidateDeleteAsync(entity, ct);
        entity.TeacherCode = teacherCode;
        entity.FullName = Required(values, "name");
        entity.Email = Email(values, "email");
        entity.PhotoDataUrl = Required(values, "photoDataUrl");
        entity.Status = status;
        Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }

    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct)
    {
        var id = entity.Id;
        if (await Db.Departments.AnyAsync(x => x.HeadTeacherId == id, ct) || await Db.Courses.AnyAsync(x => x.TeacherId == id && x.IsActive, ct) || await Db.ScheduleEntries.AnyAsync(x => x.TeacherId == id && x.Status != "Cancelled", ct)) throw new InvalidOperationException("Teacher is still assigned as a department head, course teacher, or timetable teacher.");
    }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Teachers.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Teacher)entity).Status = "Inactive"; Touch(entity); }
    protected override IManagementItemDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new TeacherResponseDto(id, new TeacherValuesDto(
            Get(values, "photoDataUrl"),
            Get(values, "teacherCode"),
            Get(values, "name"),
            Get(values, "email"),
            Get(values, "departmentId"),
            Get(values, "department"),
            Get(values, "status", "Available"),
            Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

    private static string TeacherStatus(Dictionary<string, string> values) =>
        OneOf(values, "status", "Available", "Available", "Teaching", "Meeting", "On leave", "Inactive");
}
