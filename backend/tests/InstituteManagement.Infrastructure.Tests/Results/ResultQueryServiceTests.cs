using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Results;

public sealed class ResultQueryServiceTests
{
    [Fact]
    public async Task Results_use_course_count_and_attendance_overrides()
    {
        await using var db = new InstituteDbContext(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var retake = new Student { StudentCode = "S001", FullName = "Retake Student", DepartmentId = department.Id, YearLevel = 1 };
        var failed = new Student { StudentCode = "S002", FullName = "Failed Student", DepartmentId = department.Id, YearLevel = 2 };
        var java = new Course { CourseCode = "JAVA", Name = "Java", DepartmentId = department.Id };
        var csharp = new Course { CourseCode = "CS", Name = "C#", DepartmentId = department.Id };
        db.AddRange(department, retake, failed, java, csharp);
        db.SystemSettings.AddRange(
            new SystemSetting { Section = "academic-year", Key = "currentYear", Value = "2026–2027" },
            new SystemSetting { Section = "semester", Key = "currentTerm", Value = "Semester 1" },
            new SystemSetting { Section = "grade-rules", Key = "aMinimum", Value = "90" },
            new SystemSetting { Section = "grade-rules", Key = "bMinimum", Value = "80" },
            new SystemSetting { Section = "grade-rules", Key = "cMinimum", Value = "70" },
            new SystemSetting { Section = "grade-rules", Key = "dMinimum", Value = "60" },
            new SystemSetting { Section = "grade-rules", Key = "eMinimum", Value = "50" });
        db.GradeRecords.AddRange(
            new GradeRecord { StudentId = retake.Id, CourseId = java.Id, Score = 70, LetterGrade = "C", AcademicYear = "2026–2027", Term = "Semester 1" },
            new GradeRecord { StudentId = retake.Id, CourseId = csharp.Id, Score = 90, LetterGrade = "A", AcademicYear = "2026–2027", Term = "Semester 1" },
            new GradeRecord { StudentId = failed.Id, CourseId = java.Id, Score = 95, LetterGrade = "A", AcademicYear = "2026–2027", Term = "Semester 1" });
        for (var day = 1; day <= 7; day++) db.AttendanceRecords.Add(Absent(retake.Id, day));
        for (var day = 1; day <= 8; day++) db.AttendanceRecords.Add(Absent(failed.Id, day));
        await db.SaveChangesAsync();

        var results = await new ResultQueryService(db).GetAsync(null, null, null, null, false, CancellationToken.None);

        var retakeResult = Assert.Single(results, result => result.StudentId == retake.Id);
        Assert.Equal(160, retakeResult.TotalScore);
        Assert.Equal(80, retakeResult.Average);
        Assert.Equal(2, retakeResult.TotalCourses);
        Assert.Equal("Retake Exam", retakeResult.TotalGrade);
        Assert.Equal("Fail", Assert.Single(results, result => result.StudentId == failed.Id).TotalGrade);

        db.SystemSettings.Add(new SystemSetting { Section = "attendance-rules", Key = "autoPercentage", Value = "false" });
        await db.SaveChangesAsync();
        var resultsWithoutAttendanceRule = await new ResultQueryService(db).GetAsync(null, null, null, null, false, CancellationToken.None);
        Assert.Equal("A", Assert.Single(resultsWithoutAttendanceRule, result => result.StudentId == failed.Id).TotalGrade);

        Assert.Empty(await new ResultQueryService(db).GetAsync(null, null, null, null, true, CancellationToken.None));
        db.SystemSettings.Single(setting => setting.Section == "semester" && setting.Key == "currentTerm").Value = "Semester 2";
        await db.SaveChangesAsync();
        Assert.Equal(results.Count, (await new ResultQueryService(db).GetAsync(null, null, null, null, true, CancellationToken.None)).Count);
    }

    private static AttendanceRecord Absent(Guid studentId, int day) => new() { StudentId = studentId, Date = new DateOnly(2026, 8, day), Status = "Absent", AcademicYear = "2026–2027", Term = "Semester 1" };
}
