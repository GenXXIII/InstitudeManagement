namespace InstituteManagement.Domain.Entities;

public static class PublicAccessId
{
    private const int IdentityLength = 12;

    public static string ForStudent(Guid id) => Create("STU", id);

    public static string ForTeacher(Guid id) => Create("TEA", id);

    private static string Create(string prefix, Guid id) =>
        $"{prefix}-{id:N}"[..(prefix.Length + 1 + IdentityLength)].ToUpperInvariant();
}
