using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Attendance.ClassSessions;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Attendance;

public sealed class ClassPermissionServiceTests
{
    [Fact]
    public async Task Student_can_request_before_class_and_assigned_teacher_approval_records_permission()
    {
        await using var db = new InstituteDbContext(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var department = new Department { DepartmentCode = "DEP-P", Name = "Permission" };
        var teacher = new Teacher { TeacherCode = "TEA-P", FullName = "Teacher Permission", DepartmentId = department.Id, Department = department };
        var student = new Student { StudentCode = "STU-P", PublicId = "INK-STU-P", FullName = "Student Permission", DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning" };
        var course = new Course { CourseCode = "COU-P", Name = "Permission Course", DepartmentId = department.Id, Department = department };
        var classroom = new Classroom { ClassroomCode = "ROOM-P", Status = "Available" };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var schedule = new ScheduleEntry { TimetableCode = "TIM-P", TeacherId = teacher.Id, Teacher = teacher, CourseId = course.Id, Course = course, ClassroomId = classroom.Id, Classroom = classroom, YearLevel = 1, Shift = "Morning", DayOfWeek = today.DayOfWeek, StartsAt = new TimeOnly(23, 0), EndsAt = new TimeOnly(23, 59), Status = "Upcoming" };
        db.AddRange(department, teacher, student, course, classroom, schedule,
            new StudentEnrollment { EnrollmentCode = "ESTU-P", StudentId = student.Id, DepartmentId = department.Id, YearLevel = 1, Shift = "Morning", AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active" },
            new TimetableEnrollment { EnrollmentCode = "ETIM-P", ScheduleEntryId = schedule.Id, CourseId = course.Id, TeacherId = teacher.Id, ClassroomId = classroom.Id, YearLevel = 1, AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active" },
            new SystemSetting { Section = "system", Key = "timeZone", Value = "UTC" },
            new SystemSetting { Section = "academic-year", Key = "currentYear", Value = "2026–2027" },
            new SystemSetting { Section = "semester", Key = "currentTerm", Value = "Semester 1" });
        await db.SaveChangesAsync();
        var service = new ClassPermissionService(db, new InstituteCache());

        var requested = await service.RequestAsync(student.Id, today, "Medical appointment", CancellationToken.None);
        var approved = await service.ReviewAsync(requested.Id, teacher.Id, "Approved", CancellationToken.None);

        Assert.Equal("Approved", approved.Status);
        var attendance = Assert.Single(db.AttendanceRecords);
        Assert.Equal("Permission", attendance.Status);
        Assert.Equal("Student whole-day permission", attendance.Method);
    }
}
