using System.Text.Json;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Grades;

public sealed class GradeServiceTests
{
    [Fact]
    public async Task Submit_calculates_attendance_and_total_from_configured_components()
    {
        await using var db = CreateContext();
        var department = new Department { DepartmentCode = "DEP-1", Name = "Computing" };
        var student = new Student { StudentCode = "STU-1", FullName = "Student One", DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning" };
        var course = new Course { CourseCode = "COU-1", Name = "Programming", DepartmentId = department.Id, Department = department };
        db.AddRange(department, student, course);
        AddSetting(db, "academic-year", "currentYear", "2026–2027");
        AddSetting(db, "semester", "currentTerm", "Semester 1");
        AddSetting(db, "grade-rules", "attendanceWeight", "10");
        AddSetting(db, "grade-rules", "assignmentWeight", "20");
        AddSetting(db, "grade-rules", "midtermWeight", "20");
        AddSetting(db, "grade-rules", "finalExamWeight", "50");
        db.ClassSessionRecords.AddRange(
            Session(student, course, new DateOnly(2026, 9, 1), "Present"),
            Session(student, course, new DateOnly(2026, 9, 2), "Absent"));
        await db.SaveChangesAsync();

        await new GradeService(db, new InstituteCache()).SubmitAsync(student.Id, course.Id, 18, 16, 40, CancellationToken.None);

        var grade = Assert.Single(db.GradeRecords);
        Assert.Equal(5, grade.AttendanceScore);
        Assert.Equal(18, grade.AssignmentScore);
        Assert.Equal(16, grade.MidtermScore);
        Assert.Equal(40, grade.FinalExamScore);
        Assert.Equal(79, grade.Score);
        Assert.Equal("C", grade.LetterGrade);
    }

    private static ClassSessionRecord Session(Student student, Course course, DateOnly date, string status) => new()
    {
        ScheduleEntryId = Guid.NewGuid(),
        SessionDate = date,
        AcademicYear = "2026–2027",
        Term = "Semester 1",
        DepartmentId = student.DepartmentId!.Value,
        CourseId = course.Id,
        TeacherId = Guid.NewGuid(),
        ClassroomId = Guid.NewGuid(),
        YearLevel = 1,
        StartsAt = new TimeOnly(7, 30),
        EndsAt = new TimeOnly(9, 0),
        CourseName = course.Name,
        TeacherName = "Teacher One",
        TeacherAttendanceStatus = "Present",
        ClassroomCode = "ROOM-1",
        StudentCount = 1,
        StudentAttendanceJson = JsonSerializer.Serialize(new[] { new SessionStudentSnapshot(student.Id, student.StudentCode, student.FullName, status, status == "Present" ? "07:30" : "") })
    };

    private static void AddSetting(InstituteDbContext db, string section, string key, string value) =>
        db.SystemSettings.Add(new SystemSetting { Section = section, Key = key, Value = value });

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
