using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class TeacherSeedFactory
{
    public static Teacher[] Create(Department[] departments)
    {
        var names = new[] { "David Smith", "Anna Wilson", "John Carter", "Sarah Miller", "Mike Chen", "Maya Patel", "Oliver Brown", "Emma Davis", "Liam Martin", "Nora James", "Leo Garcia", "Ava Thompson", "Sok Dara", "Chan Vanna", "Lim Sopheak", "Kim Bopha" };
        return names.Select((name, index) => new Teacher { TeacherNumber = $"T-{index + 275:D5}", FullName = name, Email = name.ToLowerInvariant().Replace(" ", ".") + "@northstar.edu", PhotoDataUrl = SeedAvatar.Create(name, "4267b2"), DepartmentId = departments[index % departments.Length].Id, Status = index < 8 ? "Teaching" : "Available" }).ToArray();
    }
}
