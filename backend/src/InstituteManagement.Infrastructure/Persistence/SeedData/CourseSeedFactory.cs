using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class CourseSeedFactory
{
    public static Course[] Create(Department[] departments, Teacher[] teachers)
    {
        var names = new[] { "Mathematics", "Physics", "English", "Biology", "Chemistry", "Web Engineering", "Data Analytics", "Business Strategy" };
        return names.Select((name, index) => new Course { Code = $"{departments[index % departments.Length].Code}-{101 + index}", Name = name, DepartmentId = departments[index % departments.Length].Id, TeacherId = teachers[index].Id, Credits = index % 3 + 2, Capacity = 35 + (index % 3) * 5 }).ToArray();
    }
}
