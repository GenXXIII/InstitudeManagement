using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Enrollment;

internal static class EnrollmentValueParser
{
    public static async Task<Guid> RequiredDepartmentAsync(
        InstituteDbContext db,
        Dictionary<string, string> values,
        CancellationToken cancellationToken) =>
        await OptionalDepartmentAsync(db, values, cancellationToken)
        ?? throw new ArgumentException("Department is required.");

    public static async Task<Guid?> OptionalDepartmentAsync(
        InstituteDbContext db,
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var id = GuidValue(values, "departmentId", false);
        if (!id.HasValue)
        {
            return null;
        }

        if (!await db.Departments.AnyAsync(
                department => department.Id == id && department.IsActive,
                cancellationToken))
        {
            throw new ArgumentException("Department must reference an active department.");
        }

        return id;
    }

    public static Guid? GuidValue(
        IReadOnlyDictionary<string, string> values,
        string key,
        bool required)
    {
        var raw = values.GetValueOrDefault(key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (required)
            {
                throw new ArgumentException($"{key} is required.");
            }

            return null;
        }

        return Guid.TryParse(raw, out var value)
            ? value
            : throw new ArgumentException($"{key} is invalid.");
    }

    public static string Required(IReadOnlyDictionary<string, string> values, string key) =>
        !string.IsNullOrWhiteSpace(values.GetValueOrDefault(key))
            ? values[key].Trim()
            : throw new ArgumentException($"{key} is required.");

    public static int Integer(
        IReadOnlyDictionary<string, string> values,
        string key,
        int minimum,
        int maximum) =>
        int.TryParse(values.GetValueOrDefault(key), out var value)
        && value >= minimum
        && value <= maximum
            ? value
            : throw new ArgumentException($"{key} must be between {minimum} and {maximum}.");

    public static string Choice(
        IReadOnlyDictionary<string, string> values,
        string key,
        IEnumerable<string> choices,
        string? fallback = null)
    {
        var value = values.GetValueOrDefault(key, fallback ?? "");
        return choices.Contains(value, StringComparer.OrdinalIgnoreCase)
            ? choices.First(choice => choice.Equals(value, StringComparison.OrdinalIgnoreCase))
            : throw new ArgumentException($"{key} is invalid.");
    }
}
