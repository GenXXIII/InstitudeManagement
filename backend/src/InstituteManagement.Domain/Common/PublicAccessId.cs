namespace InstituteManagement.Domain.Entities;

public static class PublicAccessId
{
    public static string ForStudent(Guid enrollmentId) => ForStudentEnrollment(enrollmentId);

    public static string ForTeacher(Guid enrollmentId) => ForTeacherEnrollment(enrollmentId);

    public static string ForStudentEnrollment(Guid enrollmentId) => Create("STU", enrollmentId);

    public static string ForTeacherEnrollment(Guid enrollmentId) => Create("TEA", enrollmentId);

    public static string ForEnrollment(string prefix, Guid enrollmentId) => Create(prefix, enrollmentId);

    public static bool MatchesEnrollment(string? publicId, Guid enrollmentId) =>
        !string.IsNullOrWhiteSpace(publicId)
        && publicId.EndsWith($"-{enrollmentId:N}", StringComparison.OrdinalIgnoreCase);

    private static string Create(string prefix, Guid id)
    {
        var normalizedPrefix = prefix.Trim().TrimEnd('-', '/', '.', '_').ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedPrefix)) throw new ArgumentException("Public ID prefix is required.", nameof(prefix));
        return $"{normalizedPrefix}-{id:N}".ToUpperInvariant();
    }
}
