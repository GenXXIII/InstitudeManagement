using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class StudentSeedFactory
{
    public static Student[] Create(Department[] departments)
    {
        var firstNames = new[] { "John", "Mia", "Ethan", "Sofia", "Lucas", "Isla", "Noah", "Amelia", "James", "Lily" };
        var lastNames = new[] { "Smith", "Nguyen", "Brown", "Chen", "Wilson", "Garcia" };
        return Enumerable.Range(1, 120).Select(index => { var name = $"{firstNames[(index - 1) % firstNames.Length]} {lastNames[(index - 1) % lastNames.Length]}"; return new Student { StudentNumber = $"ST-{4700 + index:D6}", FullName = name, Email = $"student{4700 + index}@northstar.edu", DepartmentId = departments[(index - 1) % departments.Length].Id, PhotoDataUrl = SeedAvatar.Create(name, "2f72d6"), YearLevel = ((index - 1) % 4) + 1, Status = index % 29 == 0 ? "Inactive" : "Active" }; }).ToArray();
    }
}
