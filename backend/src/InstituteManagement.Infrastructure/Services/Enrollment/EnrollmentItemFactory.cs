using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Infrastructure.Services.Enrollment;

internal static class EnrollmentItemFactory
{
    public static bool Matches(string? search, params string?[] values) =>
        string.IsNullOrWhiteSpace(search)
        || values.Any(value => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);

    public static Dictionary<string, string> AssignmentValues(
        params (string Key, string Value)[] values) =>
        values.ToDictionary(value => value.Key, value => value.Value);

    public static EnrollmentItemDto Item(
        Guid id,
        params (string Key, string Value)[] values) =>
        new(id, values.ToDictionary(value => value.Key, value => value.Value));
}
