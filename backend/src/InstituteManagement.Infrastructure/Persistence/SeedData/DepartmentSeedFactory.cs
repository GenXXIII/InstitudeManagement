using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class DepartmentSeedFactory
{
    public static Department[] Create() =>
    [
        new() { Name = "Information Technology", Code = "IT" },
        new() { Name = "Accounting & Finance", Code = "ACC" },
        new() { Name = "Engineering", Code = "ENG", Head = "Dr. Helen Wong" },
        new() { Name = "Arts & Humanities", Code = "ART", Head = "Prof. Sophia Reed" },
        new() { Name = "Science", Code = "SCI", Head = "Dr. Noah Kim" }
    ];
}
