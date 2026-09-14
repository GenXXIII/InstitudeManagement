using InstituteManagement.Application.Common.LiveUpdates;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Attendance.ClassSessions;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Attendance;

public sealed class ClassSessionStartServiceTests
{
    [Fact]
    public async Task Start_is_persisted_and_idempotent_for_the_assigned_teacher()
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
        var first = await service.StartAsync(schedule.Id, teacher.Id, CancellationToken.None);
        var second = await service.StartAsync(schedule.Id, teacher.Id, CancellationToken.None);

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
