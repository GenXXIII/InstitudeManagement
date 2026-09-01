using System.Text.Json;
using System.Net.Mail;
using InstituteManagement.Application.DTOs.Management;
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
    public abstract Task<IReadOnlyList<IManagementItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct);
    public abstract Task<IManagementItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct);
    public abstract Task<IManagementItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct);
    protected abstract Task<Entity?> FindAsync(Guid id, CancellationToken ct);
    protected virtual Task ValidateDeleteAsync(Entity entity, CancellationToken ct) => Task.CompletedTask;
    protected abstract void Deactivate(Entity entity);
    protected abstract IManagementItemDto Response(Guid id, IReadOnlyDictionary<string, string> values);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await FindAsync(id, ct);
        if (entity is null) return false;
        await ValidateDeleteAsync(entity, ct);
        var subject = Subject(entity);
        Deactivate(entity);
        Db.AuditLogs.Add(new AuditLog { ResourceId = id, Type = ResourceType(Resource), Subject = subject, Action = entity is AttendanceRecord or GradeRecord ? "Removed" : "Deactivated", Details = JsonSerializer.Serialize(Snapshot(entity)) });
        await Db.SaveChangesAsync(ct);
        await Cache.InvalidateDashboardAsync(ct);
        return true;
    }

    protected async Task<IManagementItemDto> SaveCreatedAsync(Entity entity, Dictionary<string, string> values, CancellationToken ct)
    {
        Db.Add(entity);
        Db.AuditLogs.Add(Audit(entity.Id, values, "Created"));
        await Db.SaveChangesAsync(ct);
        await Cache.InvalidateDashboardAsync(ct);
        return Response(entity.Id, values);
    }

    protected async Task<IManagementItemDto> SaveUpdatedAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        Db.AuditLogs.Add(Audit(id, values, "Updated"));
        await Db.SaveChangesAsync(ct);
        await Cache.InvalidateDashboardAsync(ct);
        return Response(id, values);
    }

    protected AuditLog Audit(Guid id, Dictionary<string, string> values, string action) => new() { ResourceId = id, Type = ResourceType(Resource), Subject = values.GetValueOrDefault("name", ResourceDisplayId(values)), Action = action, Details = JsonSerializer.Serialize(values) };

    private string ResourceDisplayId(IReadOnlyDictionary<string, string> values)
    {
        var key = Resource switch
        {
            "students" => "studentCode",
            "teachers" => "teacherCode",
            "departments" => "departmentCode",
            "courses" => "courseCode",
            "classrooms" => "classroomCode",
            "timetable" => "timetableCode",
            "attendance" => "attendanceCode",
            "grades" => "gradeCode",
            _ => ""
        };
        return string.IsNullOrEmpty(key) ? Resource : values.GetValueOrDefault(key, Resource);
    }
    protected static bool Matches(string? search, params string?[] values) => string.IsNullOrWhiteSpace(search) || values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
    protected static string Required(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException($"{FieldName(key)} is required.");

    protected static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback = "") =>
        values.TryGetValue(key, out var value) ? value.Trim() : fallback;

    protected static string Email(Dictionary<string, string> values, string key)
    {
        var value = Required(values, key);
        return MailAddress.TryCreate(value, out _) ? value : throw new ArgumentException($"{FieldName(key)} must be a valid email address.");
    }

    protected static string OneOf(Dictionary<string, string> values, string key, string fallback, params string[] allowed)
    {
        var value = Get(values, key, fallback);
        return allowed.Contains(value, StringComparer.OrdinalIgnoreCase)
            ? allowed.First(item => item.Equals(value, StringComparison.OrdinalIgnoreCase))
            : throw new ArgumentException($"{FieldName(key)} must be one of: {string.Join(", ", allowed)}.");
    }

    protected static int Int(Dictionary<string, string> values, string key, int fallback)
    {
        var input = Get(values, key);
        if (string.IsNullOrWhiteSpace(input)) return fallback;
        return int.TryParse(input, out var value) ? value : throw new ArgumentException($"{FieldName(key)} must be a whole number.");
    }

    protected static int IntInRange(Dictionary<string, string> values, string key, int fallback, int minimum, int maximum)
    {
        var value = Int(values, key, fallback);
        return value >= minimum && value <= maximum
            ? value
            : throw new ArgumentException($"{FieldName(key)} must be between {minimum} and {maximum}.");
    }

    protected static decimal DecimalInRange(Dictionary<string, string> values, string key, decimal minimum, decimal maximum)
    {
        var value = decimal.TryParse(Required(values, key), out var parsed)
            ? parsed
            : throw new ArgumentException($"{FieldName(key)} must be a number.");
        return value >= minimum && value <= maximum
            ? value
            : throw new ArgumentException($"{FieldName(key)} must be between {minimum} and {maximum}.");
    }
    protected static bool Bool(Dictionary<string, string> values, string key, bool fallback)
    {
        var input = Get(values, key);
        if (string.IsNullOrWhiteSpace(input)) return fallback;
        return bool.TryParse(input, out var value) ? value : throw new ArgumentException($"{FieldName(key)} must be true or false.");
    }
    protected static void Touch(Entity entity) => entity.UpdatedAtUtc = DateTime.UtcNow;
    protected static async Task<T> RequiredEntityAsync<T>(DbSet<T> set, Guid id, CancellationToken ct) where T : Entity => await set.FindAsync([id], ct) ?? throw new KeyNotFoundException($"{typeof(T).Name} not found.");
    protected static async Task EnsureUniqueAsync<T>(IQueryable<T> duplicates, string field, CancellationToken ct)
        where T : class
    {
        if (await duplicates.AnyAsync(ct)) throw new InvalidOperationException($"{field} already exists.");
    }
    protected async Task<Guid> RelatedIdAsync<T>(Dictionary<string, string> values, string key, CancellationToken ct) where T : Entity
    {
        if (!Guid.TryParse(Required(values, key), out var id) || !await Db.Set<T>().AnyAsync(x => x.Id == id, ct)) throw new ArgumentException($"{FieldName(key)} must reference an existing {typeof(T).Name.ToLowerInvariant()}.");
        return id;
    }

    protected Task<string> ConfiguredRecordCodeAsync(
        Dictionary<string, string> values,
        string inputKey,
        string settingsSection,
        string fallbackPrefix,
        IQueryable<string> existingCodes,
        CancellationToken ct) =>
        ConfiguredCodeAsync(values, inputKey, settingsSection, fallbackPrefix, existingCodes, "codePrefix", "codeIncludeYear", "codeStartingNumber", "codePaddingWidth", "codeSeparator", ct);

    protected Task<string> ConfiguredIdentityCodeAsync(
        Dictionary<string, string> values,
        string inputKey,
        string settingsSection,
        string fallbackPrefix,
        IQueryable<string> existingCodes,
        CancellationToken ct) =>
        ConfiguredCodeAsync(values, inputKey, settingsSection, fallbackPrefix, existingCodes, "idPrefix", "includeYear", "startingNumber", "paddingWidth", "separator", ct);

    private async Task<string> ConfiguredCodeAsync(
        Dictionary<string, string> values,
        string inputKey,
        string settingsSection,
        string fallbackPrefix,
        IQueryable<string> existingCodes,
        string prefixKey,
        string includeYearKey,
        string startingNumberKey,
        string paddingWidthKey,
        string separatorKey,
        CancellationToken ct)
    {
        var supplied = Get(values, inputKey);
        if (!string.IsNullOrWhiteSpace(supplied)) return supplied;

        var format = await Db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == settingsSection)
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, ct);
        var prefix = format.GetValueOrDefault(prefixKey, fallbackPrefix).Trim().ToUpperInvariant();
        var includeYear = bool.TryParse(format.GetValueOrDefault(includeYearKey), out var configuredIncludeYear) && configuredIncludeYear;
        var separator = format.GetValueOrDefault(separatorKey, "-");
        var paddingWidth = int.TryParse(format.GetValueOrDefault(paddingWidthKey), out var configuredWidth) ? Math.Clamp(configuredWidth, 1, 12) : 4;
        var startingNumber = long.TryParse(format.GetValueOrDefault(startingNumberKey), out var configuredStart) && configuredStart >= 0 ? configuredStart : 1;
        var localYear = includeYear ? (await InstituteLocalTime.NowAsync(Db, ct)).Year.ToString() : "";
        var codeStem = includeYear ? $"{prefix}{separator}{localYear}{separator}" : $"{prefix}{separator}";
        var existing = (await existingCodes.ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var sequence = startingNumber; sequence < long.MaxValue; sequence++)
        {
            var candidate = $"{codeStem}{sequence.ToString().PadLeft(paddingWidth, '0')}";
            if (!existing.Contains(candidate)) return candidate;
        }

        throw new InvalidOperationException($"No available {FieldName(inputKey)} remains for the configured format.");
    }

    private static string FieldName(string key) => key switch
    {
        "photoDataUrl" => "Photo",
        "attendanceCode" => "AttendanceCode",
        "classroomCode" => "ClassroomCode",
        "courseCode" => "CourseCode",
        "departmentCode" => "DepartmentCode",
        "gradeCode" => "GradeCode",
        "studentCode" => "StudentCode",
        "teacherCode" => "TeacherCode",
        "timetableCode" => "TimetableCode",
        "departmentId" => "Department",
        "teacherId" => "Teacher",
        "studentId" => "Student",
        "courseId" => "Course",
        "classroomId" => "Learning space",
        "name" => "Full name",
        "email" => "Email",
        "headTeacherId" => "Head of department",
        "roomType" => "Learning-space type",
        "year" or "yearLevel" => "Year level",
        "checkedInAt" => "Check-in time",
        "dayOfWeek" => "Day",
        "startsAt" => "Start time",
        "endsAt" => "End time",
        "correctionReason" => "Correction reason",
        _ => char.ToUpperInvariant(key[0]) + key[1..]
    };

    private static string ResourceType(string resource) => resource switch { "timetable" => "Timetable", "attendance" => "Attendance", "grades" => "Grade", _ => char.ToUpperInvariant(resource[0]) + resource.TrimEnd('s')[1..] };
    private static string Subject(Entity entity) => entity switch { Student x => x.FullName, Teacher x => x.FullName, Classroom x => x.ClassroomCode, Course x => x.Name, Department x => x.Name, ScheduleEntry x => x.TimetableCode, AttendanceRecord x => x.AttendanceCode, GradeRecord x => x.GradeCode, _ => entity.Id.ToString() };
    private static object Snapshot(Entity entity) => entity switch
    {
        Student x => new { x.StudentCode, x.FullName, x.Email, x.DepartmentId, x.YearLevel, x.Shift, x.Status },
        Teacher x => new { x.TeacherCode, x.FullName, x.Email, x.DepartmentId, x.Status },
        Classroom x => new { x.ClassroomCode, x.Building, x.RoomType, x.DepartmentId, x.Capacity, x.Status, x.DeviceOnline },
        Course x => new { x.CourseCode, x.Name, x.DepartmentId, x.TeacherId, x.Capacity, x.IsActive },
        Department x => new { x.DepartmentCode, x.Name, x.HeadTeacherId, x.IsActive },
        ScheduleEntry x => new { x.TimetableCode, x.CourseId, x.TeacherId, x.ClassroomId, x.YearLevel, x.DayOfWeek, x.StartsAt, x.EndsAt, x.Status },
        AttendanceRecord x => new { x.AttendanceCode, x.StudentId, x.Date, x.CheckedInAt, x.Status, x.Method },
        GradeRecord x => new { x.GradeCode, x.StudentId, x.CourseId, x.Score, x.LetterGrade, x.Term },
        _ => new { entity.Id }
    };
}
