using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Administration;

public sealed class AcademicCalendarRolloverServiceTests
{
    [Fact]
    public async Task Semester_one_end_auto_enrolls_same_year_in_semester_two_and_preserves_old_rows()
    {
        await using var db = CreateContext();
        AddCalendar(db, "Semester 1");
        var department = Department();
        var student = Student(department, 1, "Morning", "STU-1");
        var oldEnrollment = Enrollment(student, department, 1, "Morning", "2026\u20132027", "Semester 1");
        var oldTimetable = Timetable(department, "2026\u20132027", "Semester 1");
        db.AddRange(department, student, oldEnrollment, oldTimetable);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var changed = await Service(db).ApplyAsync(new DateOnly(2027, 2, 1), CancellationToken.None);

        Assert.True(changed);
        Assert.Equal("Semester 2", Value(db, "semester", "currentTerm"));
        var enrollments = await db.StudentEnrollments.OrderBy(item => item.Semester).ToListAsync();
        Assert.Equal(2, enrollments.Count);
        Assert.Contains(enrollments, item => item.Id == oldEnrollment.Id && item.Semester == "Semester 1");
        Assert.Contains(enrollments, item =>
            item.Id != oldEnrollment.Id
            && item.AcademicYear == "2026\u20132027"
            && item.Semester == "Semester 2"
            && item.YearLevel == 1
            && item.Shift == "Morning"
            && item.Status == "Active");
        Assert.Single(db.Students);
        Assert.Equal(oldTimetable.Id, Assert.Single(db.TimetableEnrollments).Id);
    }

    [Fact]
    public async Task Semester_two_end_promotes_year_then_auto_enrolls_and_does_not_re_enroll_graduate()
    {
        await using var db = CreateContext();
        AddCalendar(db, "Semester 2");
        var department = Department();
        var firstYear = Student(department, 1, "Afternoon", "STU-1");
        var fourthYear = Student(department, 4, "Morning", "STU-4");
        db.AddRange(
            department,
            firstYear,
            fourthYear,
            Enrollment(firstYear, department, 1, "Afternoon", "2026\u20132027", "Semester 2"),
            Enrollment(fourthYear, department, 4, "Morning", "2026\u20132027", "Semester 2"));
        await db.SaveChangesAsync();

        var changed = await Service(db).ApplyAsync(new DateOnly(2027, 7, 1), CancellationToken.None);

        Assert.True(changed);
        Assert.Equal("2027\u20132028", Value(db, "academic-year", "currentYear"));
        Assert.Equal("Semester 1", Value(db, "semester", "currentTerm"));
        Assert.Equal(2, firstYear.YearLevel);
        Assert.Equal("Inactive", fourthYear.Status);
        Assert.Contains(db.StudentEnrollments, item =>
            item.StudentId == firstYear.Id
            && item.AcademicYear == "2027\u20132028"
            && item.Semester == "Semester 1"
            && item.YearLevel == 2
            && item.Shift == "Afternoon");
        Assert.DoesNotContain(db.StudentEnrollments, item =>
            item.StudentId == fourthYear.Id
            && item.AcademicYear == "2027\u20132028");
        Assert.Equal(2, db.StudentEnrollments.Count(item => item.AcademicYear == "2026\u20132027"));
    }

    private static AcademicCalendarRolloverService Service(InstituteDbContext db) => new(
        db,
        new InstituteCache(),
        new AcademicCalendarClock(db),
        new StudentAcademicYearAdvancer(db),
        new AcademicPeriodEnrollmentAdvancer(db),
        new ActivePeriodLedgerCreator(db));

    private static void AddCalendar(InstituteDbContext db, string currentTerm)
    {
        db.SystemSettings.AddRange(
            Setting("academic-year", "currentYear", "2026\u20132027"),
            Setting("academic-year", "startsOn", "2026-08-01"),
            Setting("academic-year", "endsOn", "2027-06-30"),
            Setting("semester", "currentTerm", currentTerm),
            Setting("semester", "startsOn", currentTerm == "Semester 1" ? "2026-08-01" : "2027-02-01"),
            Setting("semester", "endsOn", currentTerm == "Semester 1" ? "2027-01-31" : "2027-06-30"),
            Setting("semester", "semester1StartsOn", "2026-08-01"),
            Setting("semester", "semester1EndsOn", "2027-01-31"),
            Setting("semester", "semester2StartsOn", "2027-02-01"),
            Setting("semester", "semester2EndsOn", "2027-06-30"));
    }

    private static SystemSetting Setting(string section, string key, string value) =>
        new() { Section = section, Key = key, Value = value };

    private static Department Department() =>
        new() { DepartmentCode = "IT", Name = "Information Technology" };

    private static Student Student(Department department, int year, string shift, string code) =>
        new()
        {
            StudentCode = code,
            FullName = $"Year {year} Student",
            DepartmentId = department.Id,
            Department = department,
            YearLevel = year,
            Shift = shift
        };

    private static StudentEnrollment Enrollment(
        Student student,
        Department department,
        int year,
        string shift,
        string academicYear,
        string semester) =>
        new()
        {
            EnrollmentCode = $"{student.StudentCode}-ESTU-1",
            StudentId = student.Id,
            Student = student,
            DepartmentId = department.Id,
            Department = department,
            YearLevel = year,
            Shift = shift,
            AcademicYear = academicYear,
            Semester = semester,
            Status = "Active"
        };

    private static TimetableEnrollment Timetable(Department department, string academicYear, string semester)
    {
        var course = new Course
        {
            CourseCode = "COU-1",
            Name = "Programming",
            DepartmentId = department.Id,
            Department = department,
            YearLevel = 1,
            Semester = semester
        };
        var teacher = new Teacher { TeacherCode = "TEA-1", FullName = "Teacher", DepartmentId = department.Id };
        var classroom = new Classroom { ClassroomCode = "501", Capacity = 40, DepartmentId = department.Id };
        var schedule = new ScheduleEntry
        {
            TimetableCode = "TIM-1",
            Shift = "Morning",
            DayOfWeek = DayOfWeek.Monday,
            StartsAt = new TimeOnly(7, 30),
            EndsAt = new TimeOnly(9, 0),
            Status = "Upcoming"
        };
        return new TimetableEnrollment
        {
            EnrollmentCode = "TIM-1-ETIM-1",
            ScheduleEntryId = schedule.Id,
            ScheduleEntry = schedule,
            CourseId = course.Id,
            Course = course,
            TeacherId = teacher.Id,
            Teacher = teacher,
            ClassroomId = classroom.Id,
            Classroom = classroom,
            YearLevel = 1,
            AcademicYear = academicYear,
            Semester = semester,
            Status = "Active"
        };
    }

    private static string Value(InstituteDbContext db, string section, string key) =>
        db.SystemSettings.Single(item => item.Section == section && item.Key == key).Value;

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
