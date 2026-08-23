using System.Text.Json;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Record;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Record;

public sealed class ClassSessionRecorderServiceTests
{
    [Fact]
    public async Task Timetable_end_freezes_each_students_attendance_once()
    {
        await using var db = CreateContext();
        var department = new Department { Name = "Information Technology", DepartmentCode = "IT" };
        var teacher = new Teacher { TeacherCode = "T001", FullName = "Teacher One", DepartmentId = department.Id };
        var course = new Course { CourseCode = "IT101", Name = "Java", DepartmentId = department.Id, TeacherId = teacher.Id };
        var room = new Classroom { ClassroomCode = "101", DepartmentId = department.Id, Capacity = 40 };
        var present = new Student { StudentCode = "S001", FullName = "Present Student", DepartmentId = department.Id, YearLevel = 1 };
        var absent = new Student { StudentCode = "S002", FullName = "Absent Student", DepartmentId = department.Id, YearLevel = 1 };
        var schedule = new ScheduleEntry { CourseId = course.Id, TeacherId = teacher.Id, ClassroomId = room.Id, YearLevel = 1, DayOfWeek = DayOfWeek.Monday, StartsAt = new TimeOnly(7, 30), EndsAt = new TimeOnly(9, 0) };
        var date = new DateOnly(2026, 8, 24);
        db.AddRange(department, teacher, course, room, present, absent, schedule, new AttendanceRecord { StudentId = present.Id, Date = date, CheckedInAt = new TimeOnly(7, 25), Status = "Present", AcademicYear = "2026\u20132027", Term = "Semester 1" });
        db.SystemSettings.AddRange(Settings());
        await db.SaveChangesAsync();
        var service = new ClassSessionRecorderService(db, new InstituteCache());

        var recorded = await service.RecordCompletedAsync(date, new TimeOnly(9, 0), TimeZoneInfo.Utc, CancellationToken.None);
        var repeated = await service.RecordCompletedAsync(date, new TimeOnly(10, 0), TimeZoneInfo.Utc, CancellationToken.None);

        Assert.Equal(1, recorded);
        Assert.Equal(0, repeated);
        var session = Assert.Single(db.ClassSessionRecords);
        Assert.StartsWith("CSR-", session.ClassSessionRecordCode);
        Assert.Equal(2, session.StudentCount);
        Assert.Equal(1, session.PresentCount);
        Assert.Equal(1, session.AbsentCount);
        var students = JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(session.StudentAttendanceJson)!;
        Assert.Contains(students, x => x.StudentId == present.Id && x.Status == "Present");
        Assert.Contains(students, x => x.StudentId == absent.Id && x.Status == "Absent");
        Assert.Contains(db.Notifications, notification => notification.Title == "Daily class summary");

        var records = await new ClassSessionOperationalRecordReader(db).GetAsync(department.Id, CancellationToken.None);
        var record = Assert.Single(records);
        Assert.Equal("Session", record.Module);
        Assert.Contains(record.Activities, activity =>
            activity["Activity"] == "Student attendance" &&
            activity["Student"] == "Absent Student" &&
            activity["Attendance"] == "Absent");

        var studentRecords = await new StudentOperationalRecordReader(db).GetAsync(department.Id, CancellationToken.None);
        Assert.All(studentRecords.SelectMany(item => item.Activities), activity => Assert.Equal("Class attendance", activity["Activity"]));
        Assert.Contains(studentRecords, item => item.Subject == "Absent Student" && item.Activities.Single()["Attendance"] == "Absent");

        var teacherRecord = Assert.Single(await new TeacherOperationalRecordReader(db).GetAsync(department.Id, CancellationToken.None));
        var courseRecord = Assert.Single(await new CourseOperationalRecordReader(db).GetAsync(department.Id, CancellationToken.None));
        var classroomRecord = Assert.Single(await new ClassroomOperationalRecordReader(db).GetAsync(department.Id, CancellationToken.None));
        Assert.All(teacherRecord.Activities.Concat(courseRecord.Activities).Concat(classroomRecord.Activities), activity => Assert.Equal("Completed class", activity["Activity"]));

        var periodRecords = new OperationalRecordQueryService([new ClassSessionOperationalRecordReader(db)], db);
        Assert.Single(await periodRecords.GetAsync("sessions", null, department.Id, false, CancellationToken.None));
        Assert.Single(await periodRecords.GetAsync("sessions", null, department.Id, true, CancellationToken.None));

        db.SystemSettings.Single(x => x.Section == "semester" && x.Key == "currentTerm").Value = "Semester 2";
        await db.SaveChangesAsync();
        Assert.Empty(await periodRecords.GetAsync("sessions", null, department.Id, false, CancellationToken.None));
        Assert.Single(await periodRecords.GetAsync("sessions", null, department.Id, true, CancellationToken.None));
    }

    private static IEnumerable<SystemSetting> Settings() => new[]
    {
        new SystemSetting { Section = "academic-year", Key = "currentYear", Value = "2026\u20132027" },
        new SystemSetting { Section = "semester", Key = "currentTerm", Value = "Semester 1" },
        new SystemSetting { Section = "semester", Key = "startsOn", Value = "2026-08-03" },
        new SystemSetting { Section = "attendance-rules", Key = "autoAbsent", Value = "true" },
        new SystemSetting { Section = "notifications", Key = "dailySummary", Value = "true" }
    };
    private static InstituteDbContext CreateContext() => new(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
