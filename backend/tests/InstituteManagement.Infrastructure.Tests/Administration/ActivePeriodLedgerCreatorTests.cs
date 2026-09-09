using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Administration;

public sealed class ActivePeriodLedgerCreatorTests
{
    [Fact]
    public async Task Grade_uses_only_timetable_with_same_year_semester_shift_and_department()
    {
        await using var db = CreateContext();
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student
        {
            StudentCode = "STU-1",
            FullName = "Student One",
            DepartmentId = department.Id,
            YearLevel = 1,
            Shift = "Morning"
        };
        db.AddRange(
            department,
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
            });
        var wrongSemester = Timetable(department, "WRONG-TERM", 1, "Morning", "Semester 1");
        var wrongYear = Timetable(department, "WRONG-YEAR", 2, "Morning", "Semester 2");
        var wrongShift = Timetable(department, "WRONG-SHIFT", 1, "Afternoon", "Semester 2");
        var matching = Timetable(department, "MATCH", 1, "Morning", "Semester 2");
        db.AddRange(wrongSemester.Enrollment, wrongYear.Enrollment, wrongShift.Enrollment, matching.Enrollment);
        await db.SaveChangesAsync();

        var created = await new ActivePeriodLedgerCreator(db).CreateAsync(
            "2026\u20132027",
            "Semester 2",
            new DateOnly(2027, 2, 1),
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal((1, 1), created);
        var grade = Assert.Single(db.GradeRecords);
        Assert.Equal(matching.Course.Id, grade.CourseId);
        Assert.Equal("Semester 2", grade.Term);
        var attendance = Assert.Single(db.AttendanceRecords);
        Assert.Equal(new TimeOnly(7, 30), attendance.CheckedInAt);
    }

    private static (TimetableEnrollment Enrollment, Course Course) Timetable(
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
        var teacher = new Teacher { TeacherCode = $"TEA-{code}", FullName = code };
        var classroom = new Classroom { ClassroomCode = $"CLA-{code}", Capacity = 40 };
        var schedule = new ScheduleEntry
        {
            TimetableCode = $"TIM-{code}",
            Shift = shift,
            DayOfWeek = DayOfWeek.Monday,
            StartsAt = shift == "Afternoon" ? new TimeOnly(13, 0) : new TimeOnly(7, 30),
            EndsAt = shift == "Afternoon" ? new TimeOnly(14, 30) : new TimeOnly(9, 0),
            Status = "Upcoming"
        };
        return (new TimetableEnrollment
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
        }, course);
    }

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
