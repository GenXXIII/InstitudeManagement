using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Administration;

public sealed class AcademicCalendarRolloverServiceTests
{
    [Fact]
    public async Task Semester_one_expiry_activates_semester_two_and_preserves_old_ledgers()
    {
        await using var db = CreateContext();
        AddCalendar(db);
        var student = Student(1);
        var attendance = new AttendanceRecord { StudentId = student.Id, Date = new DateOnly(2026, 12, 18), AcademicYear = "2026\u20132027", Term = "Semester 1" };
        var grade = new GradeRecord { StudentId = student.Id, CourseId = Guid.NewGuid(), Score = 80, LetterGrade = "B", AcademicYear = "2026\u20132027", Term = "Semester 1" };
        db.AddRange(student, attendance, grade);
        await db.SaveChangesAsync();

        var changed = await new AcademicCalendarRolloverService(db, new InstituteCache()).ApplyAsync(new DateOnly(2026, 12, 19), CancellationToken.None);

        Assert.True(changed);
        Assert.Equal("Semester 2", Value(db, "semester", "currentTerm"));
        Assert.Equal("2027-01-04", Value(db, "semester", "startsOn"));
        Assert.Equal(1, student.YearLevel);
        Assert.Equal("Semester 1", attendance.Term);
        Assert.Equal("Semester 1", grade.Term);
        Assert.Contains(db.AuditLogs, x => x.Action == "Semester rollover");
    }

    [Fact]
    public async Task Semester_two_expiry_advances_year_promotes_year_one_to_three_and_keeps_year_four()
    {
        await using var db = CreateContext();
        AddCalendar(db, "Semester 2", "2027-01-04", "2027-06-18");
        var first = Student(1); var third = Student(3); var fourth = Student(4);
        db.Students.AddRange(first, third, fourth);
        await db.SaveChangesAsync();

        var changed = await new AcademicCalendarRolloverService(db, new InstituteCache()).ApplyAsync(new DateOnly(2027, 6, 19), CancellationToken.None);

        Assert.True(changed);
        Assert.Equal("2027\u20132028", Value(db, "academic-year", "currentYear"));
        Assert.Equal("Semester 1", Value(db, "semester", "currentTerm"));
        Assert.Equal(2, first.YearLevel);
        Assert.Equal(4, third.YearLevel);
        Assert.Equal(4, fourth.YearLevel);
        Assert.Contains(db.AuditLogs, x => x.Action == "Year rollover" && x.Details.Contains("Year 4 students were preserved"));
    }

    private static Student Student(int year) => new() { StudentNumber = $"S{year}{Guid.NewGuid():N}", FullName = $"Year {year} Student", DepartmentId = Guid.NewGuid(), YearLevel = year };
    private static string Value(InstituteDbContext db, string section, string key) => db.SystemSettings.Single(x => x.Section == section && x.Key == key).Value;
    private static void AddCalendar(InstituteDbContext db, string term = "Semester 1", string startsOn = "2026-08-03", string endsOn = "2026-12-18") => db.SystemSettings.AddRange(new[]
    {
        Setting("academic-year", "currentYear", "2026\u20132027"), Setting("academic-year", "startsOn", "2026-08-03"), Setting("academic-year", "endsOn", "2027-06-18"),
        Setting("semester", "currentTerm", term), Setting("semester", "startsOn", startsOn), Setting("semester", "endsOn", endsOn),
        Setting("semester", "semester1StartsOn", "2026-08-03"), Setting("semester", "semester1EndsOn", "2026-12-18"), Setting("semester", "semester2StartsOn", "2027-01-04"), Setting("semester", "semester2EndsOn", "2027-06-18")
    });
    private static SystemSetting Setting(string section, string key, string value) => new() { Section = section, Key = key, Value = value };
    private static InstituteDbContext CreateContext() => new(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
