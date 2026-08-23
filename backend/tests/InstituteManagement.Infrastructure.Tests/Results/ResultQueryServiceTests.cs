using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Results;

public sealed class ResultQueryServiceTests
{
    [Fact]
    public async Task History_returns_only_finalized_closed_semesters()
    {
        await using var db = CreateContext();
        var department = new Department { DepartmentCode = "DEP-1", Name = "Business" };
        var student = new Student { StudentCode = "STU-1", FullName = "Dara Sok", DepartmentId = department.Id, YearLevel = 1 };
        var course = new Course { CourseCode = "COU-1", Name = "Business", DepartmentId = department.Id };
        db.AddRange(department, student, course,
            new GradeRecord { StudentId = student.Id, CourseId = course.Id, Score = 75, LetterGrade = "C", AcademicYear = "2025\u20132026", Term = "Semester 2" },
            new GradeRecord { StudentId = student.Id, CourseId = course.Id, Score = 90, LetterGrade = "A", AcademicYear = "2026\u20132027", Term = "Semester 1" });
        await db.SaveChangesAsync();

        var results = await new ResultQueryService(db).GetAsync(null, null, null, null, true, CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("2025\u20132026", result.AcademicYear);
        Assert.Equal("Semester 2", result.Semester);
        Assert.NotEqual("Pending", result.TotalGrade);
    }

    private static InstituteDbContext CreateContext() => new(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
