using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Attendance;
using InstituteManagement.Infrastructure.Services.Attendance.ClassSessions;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Attendance;

public sealed class ClassAttendanceQrServiceTests
{
    [Fact]
    public async Task Enrolled_student_can_scan_rotating_qr_for_running_class()
    {
        await using var db = CreateContext();
        var seeded = await SeedRunningClassAsync(db);
        var qrGateway = new ClassAttendanceQrGateway(new EphemeralDataProtectionProvider());
        var service = new ClassAttendanceQrService(
            db,
            qrGateway,
            new AttendanceService(db, new InstituteCache()));

        var firstQr = await service.GenerateAsync(seeded.Schedule.Id, seeded.Teacher.Id, CancellationToken.None);
        var secondQr = await service.GenerateAsync(seeded.Schedule.Id, seeded.Teacher.Id, CancellationToken.None);
        var result = await service.CheckInAsync(
            seeded.Schedule.Id,
            seeded.Student.Id,
            secondQr.Payload,
            CancellationToken.None);

        Assert.NotEqual(firstQr.Payload, secondQr.Payload);
        Assert.Equal("Dynamic QR", result.Method);
        var attendance = Assert.Single(await db.AttendanceRecords.ToListAsync());
        Assert.Equal(seeded.Student.Id, attendance.StudentId);
        Assert.Equal("Dynamic QR", attendance.Method);
        Assert.Contains(attendance.Status, new[] { "Present", "Late" });
    }

    [Fact]
    public async Task Student_outside_the_class_cannot_use_its_qr()
    {
        await using var db = CreateContext();
        var seeded = await SeedRunningClassAsync(db);
        var otherDepartment = new Department { DepartmentCode = "DEP-OTHER", Name = "Other" };
        var otherStudent = new Student
        {
            StudentCode = "STU-OTHER",
            FullName = "Other Student",
            DepartmentId = otherDepartment.Id,
            Department = otherDepartment,
            YearLevel = 1,
            Shift = "Morning",
            Status = "Active"
        };
        db.AddRange(
            otherDepartment,
            otherStudent,
            new StudentEnrollment
            {
                EnrollmentCode = "ENR-OTHER",
                StudentId = otherStudent.Id,
                Student = otherStudent,
                DepartmentId = otherDepartment.Id,
                Department = otherDepartment,
                YearLevel = 1,
                Shift = "Morning",
                AcademicYear = "2026–2027",
                Semester = "Semester 1",
                Status = "Active"
            });
        await db.SaveChangesAsync();

        var qrGateway = new ClassAttendanceQrGateway(new EphemeralDataProtectionProvider());
        var service = new ClassAttendanceQrService(
            db,
            qrGateway,
            new AttendanceService(db, new InstituteCache()));
        var qr = await service.GenerateAsync(seeded.Schedule.Id, seeded.Teacher.Id, CancellationToken.None);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckInAsync(
            seeded.Schedule.Id,
            otherStudent.Id,
            qr.Payload,
            CancellationToken.None));

        Assert.Contains("different Student class", error.Message);
        Assert.Empty(await db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Assigned_teacher_can_scan_an_enrolled_students_rotating_qr()
    {
        await using var db = CreateContext();
        var seeded = await SeedRunningClassAsync(db);
        var qrGateway = new ClassAttendanceQrGateway(new EphemeralDataProtectionProvider());
        var service = new ClassAttendanceQrService(
            db,
            qrGateway,
            new AttendanceService(db, new InstituteCache()));

        var firstQr = await service.GenerateStudentAsync(
            seeded.Schedule.Id,
            seeded.Student.Id,
            CancellationToken.None);
        var secondQr = await service.GenerateStudentAsync(
            seeded.Schedule.Id,
            seeded.Student.Id,
            CancellationToken.None);
        var result = await service.TeacherCheckInAsync(
            seeded.Schedule.Id,
            seeded.Teacher.Id,
            secondQr.Payload,
            CancellationToken.None);

        Assert.NotEqual(firstQr.Payload, secondQr.Payload);
        Assert.Equal(seeded.Student.Id, result.StudentId);
        Assert.Equal("Dynamic QR", result.Method);
        Assert.Single(await db.AttendanceRecords.ToListAsync());
    }

    private static async Task<SeededClass> SeedRunningClassAsync(InstituteDbContext db)
    {
        var department = new Department { DepartmentCode = "DEP-QR", Name = "QR Department" };
        var teacher = new Teacher { TeacherCode = "TEA-QR", FullName = "QR Teacher", Status = "Active" };
        var classroom = new Classroom { ClassroomCode = "CLA-QR", Status = "Available", DeviceOnline = true };
        var course = new Course { CourseCode = "COU-QR", Name = "QR Class", DepartmentId = department.Id, Department = department };
        var student = new Student
        {
            StudentCode = "STU-QR",
            FullName = "QR Student",
            DepartmentId = department.Id,
            Department = department,
            YearLevel = 1,
            Shift = "Morning",
            Status = "Active"
        };
        var now = DateTime.UtcNow;
        var schedule = new ScheduleEntry
        {
            TimetableCode = "TIM-QR",
            TeacherId = teacher.Id,
            Teacher = teacher,
            ClassroomId = classroom.Id,
            Classroom = classroom,
            CourseId = course.Id,
            Course = course,
            YearLevel = 1,
            Shift = "Morning",
            DayOfWeek = now.DayOfWeek,
            StartsAt = TimeOnly.MinValue,
            EndsAt = TimeOnly.MaxValue,
            Status = "Upcoming"
        };
        db.AddRange(
            department,
            teacher,
            classroom,
            course,
            student,
            schedule,
            new StudentEnrollment
            {
                EnrollmentCode = "ENR-QR",
                StudentId = student.Id,
                Student = student,
                DepartmentId = department.Id,
                Department = department,
                YearLevel = 1,
                Shift = "Morning",
                AcademicYear = "2026–2027",
                Semester = "Semester 1",
                Status = "Active"
            },
            new TimetableEnrollment
            {
                EnrollmentCode = "TIM-QR-ETIM-1",
                ScheduleEntryId = schedule.Id,
                ScheduleEntry = schedule,
                TeacherId = teacher.Id,
                Teacher = teacher,
                ClassroomId = classroom.Id,
                Classroom = classroom,
                CourseId = course.Id,
                Course = course,
                YearLevel = 1,
                AcademicYear = "2026–2027",
                Semester = "Semester 1",
                Status = "Active"
            },
            new ClassSessionStart
            {
                ScheduleEntryId = schedule.Id,
                ScheduleEntry = schedule,
                TeacherId = teacher.Id,
                Teacher = teacher,
                SessionDate = DateOnly.FromDateTime(now),
                StartedAtUtc = now
            },
            new SystemSetting { Section = "system", Key = "timeZone", Value = "UTC" },
            new SystemSetting { Section = "academic-year", Key = "currentYear", Value = "2026–2027" },
            new SystemSetting { Section = "semester", Key = "currentTerm", Value = "Semester 1" });
        await db.SaveChangesAsync();
        return new SeededClass(schedule, teacher, student);
    }

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed record SeededClass(ScheduleEntry Schedule, Teacher Teacher, Student Student);
}
