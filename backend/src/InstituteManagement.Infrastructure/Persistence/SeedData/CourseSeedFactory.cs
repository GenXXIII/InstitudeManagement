using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class CourseSeedFactory
{
    public static Course[] Create(Department[] departments, Teacher[] teachers)
    {
        var names = new[] { "Java Programming", "C# Development", "English Communication", "Accounting Principles", "Web Engineering", "Data Analytics", "Academic Writing", "Financial Reporting", "Database Systems", "Network Administration", "Professional English", "Business Strategy", "Mobile Development", "Cloud Computing", "Presentation Skills", "Taxation" };
        return names.Select((name, index) => new Course { Code = $"{departments[index % departments.Length].Code}-{101 + index}", Name = name, DepartmentId = departments[index % departments.Length].Id, TeacherId = teachers[index].Id, Credits = index % 3 + 2, Capacity = 35 + (index % 3) * 5 }).ToArray();
    }
}
