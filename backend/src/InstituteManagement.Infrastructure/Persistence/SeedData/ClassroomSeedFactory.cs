using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class ClassroomSeedFactory
{
    public static Classroom[] Create(Department[] departments)
    {
        var classrooms = Enumerable.Range(1, 4).SelectMany(floor => Enumerable.Range(1, 3).Select(room => $"{floor}{room:D2}"))
            .Select((code, index) => new Classroom { Code = code, Building = "Main Building", RoomType = "Classroom", Capacity = 35 + ((index + 1) % 3) * 5, DepartmentId = departments[index % departments.Length].Id, Status = "Available", DeviceOnline = true });
        var meetingRooms = new[]
        {
            new Classroom { Code = "501", Building = "Main Building", RoomType = "Meeting Room", Capacity = 50, DepartmentId = departments[0].Id, Status = "Available", DeviceOnline = true }
        };
        return [.. classrooms, .. meetingRooms];
    }
}
