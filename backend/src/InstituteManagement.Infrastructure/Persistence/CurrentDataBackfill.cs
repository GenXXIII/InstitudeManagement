using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class CurrentDataBackfill
{
    public static async Task ApplyAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var departments = await db.Departments.ToListAsync(cancellationToken);
        var computerScience = departments.FirstOrDefault(x => x.Code == "CS"); if (computerScience is not null) { computerScience.Code = "IT"; computerScience.Name = "Information Technology"; }
        var business = departments.FirstOrDefault(x => x.Code == "BUS"); if (business is not null) { business.Code = "ACC"; business.Name = "Accounting & Finance"; }
        var teachers = await db.Teachers.ToListAsync(cancellationToken);
        foreach (var teacher in teachers.Where(x => string.IsNullOrWhiteSpace(x.PhotoDataUrl))) teacher.PhotoDataUrl = SeedAvatar.Create(teacher.FullName, "4267b2");
        var students = await db.Students.ToListAsync(cancellationToken);
        foreach (var student in students.Where(x => string.IsNullOrWhiteSpace(x.PhotoDataUrl))) student.PhotoDataUrl = SeedAvatar.Create(student.FullName, "2f72d6");
        await BackfillClassroomsAsync(db, departments, cancellationToken);
        await BackfillScheduleAsync(db, cancellationToken);
        foreach (var department in departments.Where(x => x.HeadTeacherId is null)) { var head = teachers.FirstOrDefault(x => x.DepartmentId == department.Id); if (head is not null) { department.HeadTeacherId = head.Id; department.Head = head.FullName; } }
        await BackfillSettingsAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task BackfillClassroomsAsync(InstituteDbContext db, List<Department> departments, CancellationToken cancellationToken)
    {
        var classrooms = await db.Classrooms.ToListAsync(cancellationToken);
        for (var index = 0; index < classrooms.Count; index++) classrooms[index].DepartmentId ??= departments[index % departments.Count].Id;
        var legacy = new[] { "A101", "A102", "A103", "A104", "B101", "B102", "B103", "B104", "C101", "C102", "C103", "C104" };
        if (classrooms.Count != legacy.Length || !legacy.All(code => classrooms.Any(room => room.Code == code))) return;
        var current = Enumerable.Range(1, 4).SelectMany(floor => Enumerable.Range(1, 3).Select(room => $"{floor}{room:D2}")).ToArray();
        for (var index = 0; index < legacy.Length; index++) { var room = classrooms.First(x => x.Code == legacy[index]); room.Code = current[index]; room.Building = "Main Building"; }
    }

    private static async Task BackfillScheduleAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var schedules = await db.ScheduleEntries.Where(x => x.Status != "Cancelled").ToListAsync(cancellationToken);
        if (schedules.Count == 0 || schedules.Select(x => x.DayOfWeek).Distinct().Count() != 1) return;
        var monday = schedules.OrderBy(x => x.StartsAt).ToArray();
        foreach (var entry in monday) { entry.DayOfWeek = DayOfWeek.Monday; entry.Status = "Upcoming"; }
        foreach (var day in Enumerable.Range(2, 4)) foreach (var entry in monday) db.ScheduleEntries.Add(new ScheduleEntry { CourseId = entry.CourseId, TeacherId = entry.TeacherId, ClassroomId = entry.ClassroomId, DayOfWeek = (DayOfWeek)day, StartsAt = entry.StartsAt, EndsAt = entry.EndsAt, Status = "Upcoming" });
    }

    private static async Task BackfillSettingsAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var defaults = new Dictionary<string, Dictionary<string, string>> { ["departments"] = new() { ["requireDepartmentHead"] = "true", ["allowCrossDepartmentTeaching"] = "false", ["defaultStatus"] = "Active" }, ["courses"] = new() { ["defaultCredits"] = "3", ["defaultCapacity"] = "40", ["requireAssignedTeacher"] = "true" }, ["classrooms"] = new() { ["defaultCapacity"] = "40", ["attendanceDeviceRequired"] = "true", ["allowSharedRooms"] = "false" } };
        var current = await db.SystemSettings.Select(x => new { x.Section, x.Key }).ToListAsync(cancellationToken);
        foreach (var section in defaults) foreach (var item in section.Value) if (!current.Any(x => x.Section == section.Key && x.Key == item.Key)) db.SystemSettings.Add(new SystemSetting { Section = section.Key, Key = item.Key, Value = item.Value });
    }
}
