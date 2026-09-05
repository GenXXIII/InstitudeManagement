using System.Net.Mail;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Catalog;

internal static class CatalogValidation
{
    public static string Required(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException($"{FieldName(key)} is required.");

    public static string RequiredCode(Dictionary<string, string> values, string key)
    {
        var value = Required(values, key);
        if (value.Length > 64 || value.Any(character => !char.IsLetterOrDigit(character) && character is not ('.' or '_' or '/' or '-')))
            throw new ArgumentException($"{FieldName(key)} must be 1 to 64 characters using letters, numbers, dot, underscore, slash, or hyphen.");
        return value;
    }

    public static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback = "") =>
        values.TryGetValue(key, out var value) ? value.Trim() : fallback;

    public static string Email(Dictionary<string, string> values, string key)
    {
        var value = Required(values, key);
        return MailAddress.TryCreate(value, out _)
            ? value
            : throw new ArgumentException($"{FieldName(key)} must be a valid email address.");
    }

    public static string OneOf(
        Dictionary<string, string> values,
        string key,
        string fallback,
        params string[] allowed)
    {
        var value = Get(values, key, fallback);
        return allowed.Contains(value, StringComparer.OrdinalIgnoreCase)
            ? allowed.First(item => item.Equals(value, StringComparison.OrdinalIgnoreCase))
            : throw new ArgumentException($"{FieldName(key)} must be one of: {string.Join(", ", allowed)}.");
    }

    public static int Int(Dictionary<string, string> values, string key, int fallback)
    {
        var input = Get(values, key);
        if (string.IsNullOrWhiteSpace(input)) return fallback;
        return int.TryParse(input, out var value)
            ? value
            : throw new ArgumentException($"{FieldName(key)} must be a whole number.");
    }

    public static int IntInRange(
        Dictionary<string, string> values,
        string key,
        int fallback,
        int minimum,
        int maximum)
    {
        var value = Int(values, key, fallback);
        return value >= minimum && value <= maximum
            ? value
            : throw new ArgumentException($"{FieldName(key)} must be between {minimum} and {maximum}.");
    }

    public static decimal DecimalInRange(
        Dictionary<string, string> values,
        string key,
        decimal minimum,
        decimal maximum)
    {
        var value = decimal.TryParse(Required(values, key), out var parsed)
            ? parsed
            : throw new ArgumentException($"{FieldName(key)} must be a number.");
        return value >= minimum && value <= maximum
            ? value
            : throw new ArgumentException($"{FieldName(key)} must be between {minimum} and {maximum}.");
    }

    public static bool Bool(Dictionary<string, string> values, string key, bool fallback)
    {
        var input = Get(values, key);
        if (string.IsNullOrWhiteSpace(input)) return fallback;
        return bool.TryParse(input, out var value)
            ? value
            : throw new ArgumentException($"{FieldName(key)} must be true or false.");
    }

    public static async Task<T> RequiredEntityAsync<T>(DbSet<T> set, Guid id, CancellationToken ct)
        where T : Entity =>
        await set.FindAsync([id], ct) ?? throw new KeyNotFoundException($"{typeof(T).Name} not found.");

    public static async Task EnsureUniqueAsync<T>(IQueryable<T> duplicates, string field, CancellationToken ct)
        where T : class
    {
        if (await duplicates.AnyAsync(ct)) throw new InvalidOperationException($"{field} already exists.");
    }

    public static async Task<Guid> RelatedIdAsync<T>(
        InstituteDbContext db,
        Dictionary<string, string> values,
        string key,
        CancellationToken ct)
        where T : Entity
    {
        if (!Guid.TryParse(Required(values, key), out var id) || !await db.Set<T>().AnyAsync(entity => entity.Id == id, ct))
            throw new ArgumentException($"{FieldName(key)} must reference an existing {typeof(T).Name.ToLowerInvariant()}.");
        return id;
    }

    public static string FieldName(string key) => key switch
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
}
