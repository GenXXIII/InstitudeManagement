using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Management.Timetable;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Management;

public sealed class TimetableRoomRulesTests
{
    [Fact]
    public async Task Classroom_501_rejects_year_two_to_four()
    {
        await using var db = CreateContext();
        var department = new Department { DepartmentCode = "DEP-1", Name = "Business" };
        var teacher = new Teacher { TeacherCode = "TEA-1", FullName = "Dara Sok", DepartmentId = department.Id };
        var course = new Course { CourseCode = "COU-1", Name = "Business", DepartmentId = department.Id, TeacherId = teacher.Id };
        var room = new Classroom { ClassroomCode = "501", Building = "INK Academic Building", RoomType = "Meeting Room", Capacity = 24 };
        db.AddRange(department, teacher, course, room);
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new TimetableManagementFeature(db, new InstituteCache()).CreateAsync(new Dictionary<string, string>
        {
            ["timetableCode"] = "TIM-1", ["courseId"] = course.Id.ToString(), ["teacherId"] = teacher.Id.ToString(),
            ["classroomId"] = room.Id.ToString(), ["yearLevel"] = "2", ["dayOfWeek"] = "Monday",
            ["startsAt"] = "07:30", ["endsAt"] = "09:00", ["status"] = "Upcoming"
        }, CancellationToken.None));

        Assert.Equal("Classroom 501 is reserved for Year 1 timetable entries only.", error.Message);
    }

    private static InstituteDbContext CreateContext() => new(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
