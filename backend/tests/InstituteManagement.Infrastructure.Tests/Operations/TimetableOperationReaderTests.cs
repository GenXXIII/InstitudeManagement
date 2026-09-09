using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Operations;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Operations;

public sealed class TimetableOperationReaderTests
{
    [Fact]
    public async Task Weekly_rows_require_matching_student_year_semester_shift_and_department()
    {
        await using var db = CreateContext();
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var otherDepartment = new Department { DepartmentCode = "FIN", Name = "Finance" };
        var student = new Student
        {
            StudentCode = "STU-1",
            FullName = "Student One",
            DepartmentId = department.Id,
            YearLevel = 1,
            Shift = "Morning"
        };
        var matchingTimetable = Timetable(department, "MATCH-1", 1, "Morning", "Semester 2");
        db.AddRange(
            department,
            otherDepartment,
            student,
            new StudentEnrollment
            {
                EnrollmentCode = "STU-1-ESTU-1",
                StudentId = student.Id,
                DepartmentId = department.Id,
                YearLevel = 1,
                Shift = "Morning",
                AcademicYear = "2026\u20132027",
                Semester = "Semester 2",
                Status = "Active"
            },
            Setting("academic-year", "currentYear", "2026\u20132027"),
            Setting("semester", "currentTerm", "Semester 2"),
            matchingTimetable,
            Timetable(department, "WRONG-YEAR-2", 2, "Morning", "Semester 2"),
            Timetable(department, "WRONG-SHIFT-3", 1, "Afternoon", "Semester 2"),
            Timetable(otherDepartment, "WRONG-DEPARTMENT-4", 1, "Morning", "Semester 2"),
            Timetable(department, "WRONG-TERM-5", 1, "Morning", "Semester 1"),
            new CourseAssignment
            {
                EnrollmentCode = "COU-MATCH-1-ECOU-1",
                CourseId = matchingTimetable.CourseId,
                Course = matchingTimetable.Course,
                DepartmentId = otherDepartment.Id,
                Department = otherDepartment,
                YearLevel = 1,
                AcademicYear = "2026\u20132027",
                Semester = "Semester 2",
                Status = "Active"
            });
        await db.SaveChangesAsync();

        var result = await new TimetableOperationReader(
                db,
                new OperationContextService(db),
                new OperationEnrollmentSourceService(db, new OperationEnrollmentPeriodService(db)))
            .GetAsync(null, CancellationToken.None);

        var row = Assert.Single(result.WeeklySchedule!);
        Assert.Equal("TIM-MATCH-1", row.TimetableCode);
        Assert.Equal(1, row.YearLevel);
        Assert.Equal("Morning", row.Session);
    }

    private static TimetableEnrollment Timetable(
        Department department,
        string code,
        int year,
        string shift,
        string semester)
    {
        var course = new Course
        {
            CourseCode = $"COU-{code}",
            Name = code,
            DepartmentId = department.Id,
            YearLevel = year,
            Semester = semester
        };
        var teacher = new Teacher { TeacherCode = $"TEA-{code}", FullName = code, Status = "Active" };
        var classroom = new Classroom
        {
            ClassroomCode = $"CLA-{code}",
            Capacity = 40,
            Status = "Available",
            DeviceOnline = true
        };
        var schedule = new ScheduleEntry
        {
            TimetableCode = $"TIM-{code}",
            Shift = shift,
            DayOfWeek = DayOfWeek.Monday,
            StartsAt = shift == "Afternoon" ? new TimeOnly(13, 0) : new TimeOnly(7, 30),
            EndsAt = shift == "Afternoon" ? new TimeOnly(14, 30) : new TimeOnly(9, 0),
            Status = "Upcoming"
        };
        return new TimetableEnrollment
        {
            EnrollmentCode = $"TIM-{code}-ETIM-1",
            ScheduleEntryId = schedule.Id,
            ScheduleEntry = schedule,
            CourseId = course.Id,
            Course = course,
            TeacherId = teacher.Id,
            Teacher = teacher,
            ClassroomId = classroom.Id,
            Classroom = classroom,
            YearLevel = year,
            AcademicYear = "2026\u20132027",
            Semester = semester,
            Status = "Active"
        };
    }

    private static SystemSetting Setting(string section, string key, string value) =>
        new() { Section = section, Key = key, Value = value };

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
