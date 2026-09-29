namespace InstituteManagement.Domain.Common;

public static class AttendanceIdentityQr
{
    private const string Prefix = "INK-ATTENDANCE";

    public static string CreateStudent(string? publicId) => Create("STUDENT", publicId);

    public static string CreateTeacher(string? publicId) => Create("TEACHER", publicId);

    public static string ReadStudent(string? payload) => Read(payload, "STUDENT");

    public static string ReadTeacher(string? payload) => Read(payload, "TEACHER");

    private static string Create(string role, string? publicId)
    {
        var normalized = NormalizePublicId(publicId);
        return normalized.Length == 0 ? string.Empty : $"{Prefix}:{role}:{normalized}";
    }

    private static string Read(string? payload, string expectedRole)
    {
        var parts = payload?.Trim().Split(':', 3, StringSplitOptions.TrimEntries) ?? [];
        if (parts.Length != 3
            || !parts[0].Equals(Prefix, StringComparison.Ordinal)
            || !parts[1].Equals(expectedRole, StringComparison.Ordinal)
            || NormalizePublicId(parts[2]).Length == 0)
        {
            throw new ArgumentException($"This is not a valid {expectedRole.ToLowerInvariant()} attendance QR.");
        }

        return NormalizePublicId(parts[2]);
    }

    private static string NormalizePublicId(string? publicId) =>
        publicId?.Trim().ToUpperInvariant() ?? string.Empty;
}
