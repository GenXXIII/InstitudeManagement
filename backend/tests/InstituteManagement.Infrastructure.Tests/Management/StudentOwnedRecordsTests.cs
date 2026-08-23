using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Management.Attendance;
using InstituteManagement.Infrastructure.Services.Management.Grades;
using InstituteManagement.Infrastructure.Services.Management.Students;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Management;

public sealed class StudentOwnedRecordsTests
{
    [Fact]
    public async Task Creating_a_student_creates_one_attendance_and_one_grade()
    {
        await using var db = CreateContext();
        var department = new Department { DepartmentCode = "DEP-1", Name = "Business Administration" };
        var teacher = new Teacher { TeacherCode = "TEA-1", FullName = "Dara Sok", DepartmentId = department.Id };
        var course = new Course { CourseCode = "COU-1", Name = "Introduction to Business", DepartmentId = department.Id, TeacherId = teacher.Id };
        var room = new Classroom { ClassroomCode = "101", Building = "INK Academic Building", Capacity = 40 };
        var schedule = new ScheduleEntry
        {
            TimetableCode = "TIM-1", CourseId = course.Id, TeacherId = teacher.Id, ClassroomId = room.Id,
            YearLevel = 1, DayOfWeek = DayOfWeek.Monday, StartsAt = new TimeOnly(7, 30), EndsAt = new TimeOnly(9, 0)
        };
        db.AddRange(department, teacher, course, room, schedule);
        await db.SaveChangesAsync();
        var service = new StudentManagementFeature(db, new InstituteCache());

        await service.CreateAsync(new Dictionary<string, string>
        {
            ["studentCode"] = "STU-801", ["name"] = "Sok Dara", ["email"] = "sok.dara.stu801@gmail.com",
            ["photoDataUrl"] = "data:image/png;base64,AA==", ["departmentId"] = department.Id.ToString(),
            ["year"] = "1", ["shift"] = "Morning", ["status"] = "Active"
        }, CancellationToken.None);

        var student = Assert.Single(await db.Students.ToListAsync());
        Assert.Equal("Morning", student.Shift);
        Assert.Equal("ATT-801", Assert.Single(await db.AttendanceRecords.ToListAsync()).AttendanceCode);
        var grade = Assert.Single(await db.GradeRecords.ToListAsync());
        Assert.Equal("GRD-801", grade.GradeCode);
        Assert.Equal(course.Id, grade.CourseId);
    }

    [Fact]
    public async Task Attendance_and_grades_cannot_be_created_manually()
    {
        await using var db = CreateContext();
        var cache = new InstituteCache();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new AttendanceManagementFeature(db, cache).CreateAsync([], CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new GradeManagementFeature(db, cache).CreateAsync([], CancellationToken.None));
    }

    private static InstituteDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InstituteDbContext(options);
    }
}
