using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Results;

public sealed class ResultQueryServiceTests
{
    [Fact]
    public async Task Five_approved_grades_can_be_published_and_are_returned_as_history()
    {
        await using var db = new InstituteDbContext(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var department = new Department { DepartmentCode = "DEP-R", Name = "Results" };
        var teacher = new Teacher { TeacherCode = "TEA-R", FullName = "Teacher Results" };
        var student = new Student { StudentCode = "STU-R", FullName = "Student Results", DepartmentId = department.Id, Department = department, YearLevel = 2, Shift = "Morning" };
        db.AddRange(department, teacher, student,
            new SystemSetting { Section = "academic-year", Key = "currentYear", Value = "2026–2027" },
            new SystemSetting { Section = "semester", Key = "currentTerm", Value = "Semester 1" });
        for (var index = 1; index <= 5; index++)
        {
            var course = new Course { CourseCode = $"COU-R{index}", Name = $"Course {index}", DepartmentId = department.Id, Department = department };
            db.Add(course);
            db.GradeRecords.Add(new GradeRecord { GradeCode = $"GRA-R{index}", StudentId = student.Id, CourseId = course.Id, AcademicYear = "2026–2027", Term = "Semester 1", Score = 80, LetterGrade = "B", ReviewStatus = "Approved", SubmittedByTeacherId = teacher.Id });
        }
        await db.SaveChangesAsync();
        var service = new ResultQueryService(db);

        await service.PublishAsync(student.Id, "2026–2027", "Semester 1", CancellationToken.None);
        var history = await service.GetAsync(null, null, null, null, true, student.Id, true, CancellationToken.None);

        var result = Assert.Single(history);
        Assert.True(result.IsPublished);
        Assert.Equal("Published", result.PublicationStatus);
        Assert.Equal(5, result.TotalCourses);
        Assert.Equal(400, result.TotalScore);
    }
}
