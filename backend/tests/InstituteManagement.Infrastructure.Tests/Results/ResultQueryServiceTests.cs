using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using InstituteManagement.Infrastructure.Services.Finance;
using InstituteManagement.Infrastructure.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Results;

public sealed class ResultQueryServiceTests
{
    [Fact]
    public async Task Declare_all_keeps_current_results_read_only_then_archives_them_after_the_semester_ends()
    {
        await using var db = new InstituteDbContext(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var department = new Department { DepartmentCode = "DEP-R", Name = "Results" };
        var teacher = new Teacher { TeacherCode = "TEA-R", FullName = "Teacher Results" };
        var student = new Student { StudentCode = "STU-R", FullName = "Student Results", DepartmentId = department.Id, Department = department, YearLevel = 2, Shift = "Morning" };
        var enrollment = new StudentEnrollment { EnrollmentCode = "STU-R-ESTU-1", StudentId = student.Id, Student = student, DepartmentId = department.Id, Department = department, YearLevel = 2, Shift = "Evening", AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active" };
        db.AddRange(department, teacher, student, enrollment,
            new SystemSetting { Section = "academic-year", Key = "currentYear", Value = "2026–2027" },
            new SystemSetting { Section = "semester", Key = "currentTerm", Value = "Semester 1" });
        for (var index = 1; index <= 5; index++)
        {
            var course = new Course { CourseCode = $"COU-R{index}", Name = $"Course {index}", YearLevel = 2, Semester = "Semester 1", DepartmentId = department.Id, Department = department };
            db.Add(course);
            db.GradeRecords.Add(new GradeRecord { GradeCode = $"GRA-R{index}", StudentId = student.Id, CourseId = course.Id, AcademicYear = "2026–2027", Term = "Semester 1", Score = 80, LetterGrade = "B", ReviewStatus = "Approved", SubmittedByTeacherId = teacher.Id, SubmittedAtUtc = DateTime.UtcNow, FinalizedAtUtc = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
        var service = new ResultQueryService(db, new FinancialProgression(db, new ActivePeriodLedgerCreator(db)));

        var current = Assert.Single(await service.GetAsync(null, null, null, null, false, CancellationToken.None));
        Assert.Equal("Ready", current.PublicationStatus);
        Assert.Equal("Evening", current.Shift);
        Assert.Equal(5, current.Grades.Count);
        Assert.Equal(5, current.ExpectedCourseCount);
        Assert.Equal(10, current.AttendanceScore);
        Assert.Equal("A", current.AttendanceGrade);
        Assert.Equal("B", current.OverallGrade);
        Assert.Equal("RES-1-STU-R", current.ResultCode);
        Assert.All(current.Grades, grade => Assert.True(grade.IsApproved));

        Assert.Equal(1, await service.PublishAllAsync(null, null, CancellationToken.None));
        var declared = Assert.Single(await service.GetAsync(null, null, null, null, false, CancellationToken.None));
        Assert.Equal("Declared", declared.PublicationStatus);
        Assert.Empty(await service.GetAsync(null, null, null, null, true, student.Id, true, CancellationToken.None));
        db.SystemSettings.Single(item => item.Section == "semester" && item.Key == "currentTerm").Value = "Semester 2";
        await db.SaveChangesAsync();
        var history = await service.GetAsync(null, null, null, null, true, student.Id, true, CancellationToken.None);

        var result = Assert.Single(history);
        Assert.True(result.IsPublished);
        Assert.Equal("Declared", result.PublicationStatus);
        Assert.Equal(5, result.TotalCourses);
        Assert.Equal(400, result.TotalScore);
    }
}
