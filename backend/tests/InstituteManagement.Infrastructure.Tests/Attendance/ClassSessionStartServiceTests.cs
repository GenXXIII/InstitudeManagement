using InstituteManagement.Application.Common.LiveUpdates;
using InstituteManagement.Domain.Common;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Attendance.ClassSessions;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Attendance;

public sealed class ClassSessionStartServiceTests
{
    [Fact]
    public async Task Teacher_must_scan_own_public_id_qr_before_class_is_started()
    {
        await using var db = CreateContext();
        var teacher = new Teacher { TeacherCode = "TEA-START", FullName = "Teacher Start", Status = "Active" };
        var classroom = new Classroom { ClassroomCode = "CLA-START", Status = "Available", DeviceOnline = true };
        var course = new Course { CourseCode = "COU-START", Name = "Class Start" };
        var now = DateTime.UtcNow;
        var schedule = new ScheduleEntry
        {
            TimetableCode = "TIM-START",
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
            teacher,
            classroom,
            course,
            schedule,
            new TeacherAssignment
            {
                EnrollmentCode = "ENR-TEA-START",
                PublicId = "TEA-START-PUBLIC-ID",
                TeacherId = teacher.Id,
                Teacher = teacher,
                AcademicYear = "2026\u20132027",
                Semester = "Semester 1",
                Status = "Assigned"
            },
            new TimetableEnrollment
            {
                EnrollmentCode = "TIM-START-ETIM-1",
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
            new SystemSetting { Section = "system", Key = "timeZone", Value = "UTC" });
        await db.SaveChangesAsync();

        var publisher = new CapturingPublisher();
        var service = new ClassSessionStartService(db, new InstituteCache(), publisher);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartWithAttendanceQrAsync(
            schedule.Id,
            teacher.Id,
            AttendanceIdentityQr.CreateTeacher("TEA-OTHER-PUBLIC-ID"),
            CancellationToken.None));
        Assert.Empty(await db.ClassSessionStarts.ToListAsync());

        var qrPayload = AttendanceIdentityQr.CreateTeacher("TEA-START-PUBLIC-ID");
        var first = await service.StartWithAttendanceQrAsync(schedule.Id, teacher.Id, qrPayload, CancellationToken.None);
        var second = await service.StartWithAttendanceQrAsync(schedule.Id, teacher.Id, qrPayload, CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(schedule.Id, first.ScheduleEntryId);
        Assert.Single(await db.ClassSessionStarts.ToListAsync());
        Assert.Equal("CLASS_STARTED", Assert.Single(publisher.Events));
    }

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class CapturingPublisher : ILiveUpdatePublisher
    {
        public List<string> Events { get; } = [];

        public Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken)
        {
            Events.Add(eventName);
            return Task.CompletedTask;
        }
    }
}
