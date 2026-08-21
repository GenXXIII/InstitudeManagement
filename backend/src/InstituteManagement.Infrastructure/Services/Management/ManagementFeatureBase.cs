using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management;

public abstract class ManagementFeatureBase(InstituteDbContext db, InstituteCache cache) : IManagementFeature
{
    protected InstituteDbContext Db { get; } = db;
    protected InstituteCache Cache { get; } = cache;
    public abstract string Resource { get; }
    public abstract Task<IReadOnlyList<CatalogItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct);
    public abstract Task<CatalogItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct);
    public abstract Task<CatalogItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct);
    protected abstract Task<Entity?> FindAsync(Guid id, CancellationToken ct);
    protected virtual Task ValidateDeleteAsync(Entity entity, CancellationToken ct) => Task.CompletedTask;
    protected abstract void Deactivate(Entity entity);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await FindAsync(id, ct);
        if (entity is null) return false;
        await ValidateDeleteAsync(entity, ct);
        var subject = Subject(entity);
        Deactivate(entity);
        Db.AuditLogs.Add(new AuditLog { ResourceId = id, Type = ResourceType(Resource), Subject = subject, Action = entity is AttendanceRecord or GradeRecord ? "Removed" : "Deactivated", Details = JsonSerializer.Serialize(Snapshot(entity)) });
        await Db.SaveChangesAsync(ct);
        await Cache.InvalidateDashboardAsync();
        return true;
    }

    protected async Task<CatalogItemDto> SaveCreatedAsync(Entity entity, Dictionary<string, string> values, CancellationToken ct)
    {
        Db.Add(entity);
        Db.AuditLogs.Add(Audit(entity.Id, values, "Created"));
        await Db.SaveChangesAsync(ct);
        await Cache.InvalidateDashboardAsync();
        return Item(entity.Id, values);
    }

    protected async Task<CatalogItemDto> SaveUpdatedAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        Db.AuditLogs.Add(Audit(id, values, "Updated"));
        await Db.SaveChangesAsync(ct);
        await Cache.InvalidateDashboardAsync();
        return Item(id, values);
    }

    protected AuditLog Audit(Guid id, Dictionary<string, string> values, string action) => new() { ResourceId = id, Type = ResourceType(Resource), Subject = values.GetValueOrDefault("name", values.GetValueOrDefault("code", values.GetValueOrDefault("number", Resource))), Action = action, Details = JsonSerializer.Serialize(values) };
    protected static CatalogItemDto Item(Guid id, Dictionary<string, string> values) => new(id, new(values));
    protected static CatalogItemDto Item(Guid id, params (string Key, string Value)[] values) => new(id, values.ToDictionary(x => x.Key, x => x.Value));
    protected static bool Matches(string? search, params string?[] values) => string.IsNullOrWhiteSpace(search) || values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
    protected static string Required(Dictionary<string, string> values, string key) => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException($"{key} is required.");
    protected static string Get(Dictionary<string, string> values, string key, string fallback = "") => values.GetValueOrDefault(key, fallback);
    protected static int Int(Dictionary<string, string> values, string key, int fallback) => int.TryParse(Get(values, key), out var value) ? value : fallback;
    protected static decimal Decimal(Dictionary<string, string> values, string key) => decimal.TryParse(Required(values, key), out var value) ? value : throw new ArgumentException($"{key} must be a number.");
    protected static bool Bool(Dictionary<string, string> values, string key, bool fallback) => bool.TryParse(Get(values, key), out var value) ? value : fallback;
    protected static void Touch(Entity entity) => entity.UpdatedAtUtc = DateTime.UtcNow;
    protected static async Task<T> RequiredEntityAsync<T>(DbSet<T> set, Guid id, CancellationToken ct) where T : Entity => await set.FindAsync([id], ct) ?? throw new KeyNotFoundException($"{typeof(T).Name} not found.");
    protected async Task<Guid> RelatedIdAsync<T>(Dictionary<string, string> values, string key, CancellationToken ct) where T : Entity
    {
        if (!Guid.TryParse(Required(values, key), out var id) || !await Db.Set<T>().AnyAsync(x => x.Id == id, ct)) throw new ArgumentException($"{key} does not reference an existing {typeof(T).Name.ToLowerInvariant()}.");
        return id;
    }

    private static string ResourceType(string resource) => resource switch { "timetable" => "Timetable", "attendance" => "Attendance", "grades" => "Grade", _ => char.ToUpperInvariant(resource[0]) + resource.TrimEnd('s')[1..] };
    private static string Subject(Entity entity) => entity switch { Student x => x.FullName, Teacher x => x.FullName, Classroom x => x.Code, Course x => x.Name, Department x => x.Name, ScheduleEntry x => x.Id.ToString(), AttendanceRecord x => x.Id.ToString(), GradeRecord x => x.Id.ToString(), _ => entity.Id.ToString() };
    private static object Snapshot(Entity entity) => entity switch
    {
        Student x => new { x.StudentNumber, x.FullName, x.Email, x.DepartmentId, x.YearLevel, x.Status },
        Teacher x => new { x.TeacherNumber, x.FullName, x.Email, x.DepartmentId, x.Status },
        Classroom x => new { x.Code, x.Building, x.DepartmentId, x.Capacity, x.Status, x.DeviceOnline },
        Course x => new { x.Code, x.Name, x.DepartmentId, x.TeacherId, x.Credits, x.Capacity, x.IsActive },
        Department x => new { x.Code, x.Name, x.HeadTeacherId, x.IsActive },
        ScheduleEntry x => new { x.CourseId, x.TeacherId, x.ClassroomId, x.DayOfWeek, x.StartsAt, x.EndsAt, x.Status },
        AttendanceRecord x => new { x.StudentId, x.Date, x.CheckedInAt, x.Status, x.Method },
        GradeRecord x => new { x.StudentId, x.CourseId, x.Score, x.LetterGrade, x.Term },
        _ => new { entity.Id }
    };
}
