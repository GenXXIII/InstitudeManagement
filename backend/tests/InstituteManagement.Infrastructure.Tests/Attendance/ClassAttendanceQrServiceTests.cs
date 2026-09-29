using InstituteManagement.Domain.Common;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Attendance;
using InstituteManagement.Infrastructure.Services.Attendance.ClassSessions;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Attendance;

public sealed class ClassAttendanceQrServiceTests
{
    [Fact]
    public async Task Enrolled_student_scans_own_public_id_qr_and_becomes_present()
    {
        await using var db = CreateContext();
        var seeded = await SeedRunningClassAsync(db);
        var service = CreateService(db);

        var result = await service.CheckInAsync(
            seeded.Schedule.Id,
            seeded.Student.Id,
            AttendanceIdentityQr.CreateStudent(seeded.Enrollment.PublicId),
            CancellationToken.None);

        Assert.Equal("Present", result.Status);
        Assert.Equal("Public ID QR", result.Method);
        var attendance = Assert.Single(await db.AttendanceRecords.ToListAsync());
        Assert.Equal(seeded.Student.Id, attendance.StudentId);
        Assert.Equal("Present", attendance.Status);
        Assert.Equal("Public ID QR", attendance.Method);
    }

    [Fact]
    public async Task Student_cannot_scan_another_students_public_id_qr()
    {
        await using var db = CreateContext();
        var seeded = await SeedRunningClassAsync(db);
        var service = CreateService(db);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckInAsync(
            seeded.Schedule.Id,
            seeded.Student.Id,
            AttendanceIdentityQr.CreateStudent("STU-OTHER-PUBLIC-ID"),
            CancellationToken.None));

        Assert.Contains("signed-in Student Public ID", error.Message);
        Assert.Empty(await db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Student_cannot_use_a_teacher_attendance_qr()
    {
        await using var db = CreateContext();
        var seeded = await SeedRunningClassAsync(db);
        var service = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CheckInAsync(
            seeded.Schedule.Id,
            seeded.Student.Id,
            AttendanceIdentityQr.CreateTeacher("TEA-PUBLIC-ID"),
            CancellationToken.None));

        Assert.Empty(await db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Year_one_student_can_attend_a_general_course_from_another_department()
    {
        await using var db = CreateContext();
        var seeded = await SeedRunningClassAsync(db);
        var otherDepartment = new Department { DepartmentCode = "DEP-GENERAL", Name = "General Student Department" };
        var generalStudent = new Student
        {
            StudentCode = "STU-GENERAL",
            FullName = "General Year One Student",
            DepartmentId = otherDepartment.Id,
            Department = otherDepartment,
            YearLevel = 1,
            Shift = "Morning",
            Status = "Active"
        };
        var enrollment = new StudentEnrollment
        {
            EnrollmentCode = "ENR-GENERAL",
            PublicId = "STU-GENERAL-PUBLIC-ID",
            StudentId = generalStudent.Id,
            Student = generalStudent,
            DepartmentId = otherDepartment.Id,
            Department = otherDepartment,
            YearLevel = 1,
            Shift = "Morning",
            AcademicYear = "2026\u20132027",
            Semester = "Semester 1",
            Status = "Active"
        };
        db.AddRange(otherDepartment, generalStudent, enrollment);
        await db.SaveChangesAsync();

        var result = await CreateService(db).CheckInAsync(
            seeded.Schedule.Id,
            generalStudent.Id,
            AttendanceIdentityQr.CreateStudent(enrollment.PublicId),
            CancellationToken.None);

        Assert.Equal(generalStudent.Id, result.StudentId);
        Assert.Equal("Present", result.Status);
    }

    [Fact]
    public async Task Student_outside_the_class_year_cannot_attend()
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
            YearLevel = 2,
            Shift = "Morning",
            Status = "Active"
        };
        var enrollment = new StudentEnrollment
        {
            EnrollmentCode = "ENR-OTHER",
            PublicId = "STU-OTHER-PUBLIC-ID",
            StudentId = otherStudent.Id,
            Student = otherStudent,
            DepartmentId = otherDepartment.Id,
            Department = otherDepartment,
            YearLevel = 2,
            Shift = "Morning",
            AcademicYear = "2026\u20132027",
            Semester = "Semester 1",
            Status = "Active"
        };
        db.AddRange(otherDepartment, otherStudent, enrollment);
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(db).CheckInAsync(
            seeded.Schedule.Id,
            otherStudent.Id,
            AttendanceIdentityQr.CreateStudent(enrollment.PublicId),
            CancellationToken.None));

        Assert.Contains("different Student class", error.Message);
        Assert.Empty(await db.AttendanceRecords.ToListAsync());
    }

    private static ClassAttendanceQrService CreateService(InstituteDbContext db) =>
        new(db, new AttendanceService(db, new InstituteCache()));

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
        var enrollment = new StudentEnrollment
        {
            EnrollmentCode = "ENR-QR",
            PublicId = "STU-QR-PUBLIC-ID",
            StudentId = student.Id,
            Student = student,
            DepartmentId = department.Id,
            Department = department,
            YearLevel = 1,
            Shift = "Morning",
            AcademicYear = "2026\u20132027",
            Semester = "Semester 1",
            Status = "Active"
        };
        db.AddRange(
            department,
            teacher,
            classroom,
            course,
            student,
            schedule,
            enrollment,
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
                AcademicYear = "2026\u20132027",
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
            new SystemSetting { Section = "academic-year", Key = "currentYear", Value = "2026\u20132027" },
            new SystemSetting { Section = "semester", Key = "currentTerm", Value = "Semester 1" });
        await db.SaveChangesAsync();
        return new SeededClass(schedule, student, enrollment);
    }

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed record SeededClass(ScheduleEntry Schedule, Student Student, StudentEnrollment Enrollment);
}
