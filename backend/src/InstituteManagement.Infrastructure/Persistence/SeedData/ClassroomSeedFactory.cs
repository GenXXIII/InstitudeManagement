using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class ClassroomSeedFactory
{
    public static Classroom[] Create(Department[] departments)
    {
        var codes = Enumerable.Range(1, 4).SelectMany(floor => Enumerable.Range(1, 3).Select(room => $"{floor}{room:D2}")).ToArray();
        return codes.Select((code, index) => new Classroom { Code = code, Building = "Main Building", Capacity = 35 + ((index + 1) % 3) * 5, DepartmentId = departments[index % departments.Length].Id, Status = index < 6 ? "Running" : index < 9 ? "Available" : index == 11 ? "Offline" : "Starting", DeviceOnline = index != 11 }).ToArray();
    }
}
