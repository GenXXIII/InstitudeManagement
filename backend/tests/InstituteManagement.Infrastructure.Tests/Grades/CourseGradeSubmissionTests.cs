using InstituteManagement.Application.Features.Grades;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Grades;

public sealed class CourseGradeSubmissionTests
{
    [Fact]
    public async Task Complete_course_roster_moves_through_one_approval_and_submission_workflow()
    {
        await using var db = CreateContext();
        var setup = await SeedCourseRosterAsync(db);
        var service = new GradeService(db, new InstituteCache());

        await service.RequestCourseSubmissionAsync(setup.Teacher.Id, setup.Course.Id,
        [
            new GradeStudentScore(setup.Students[0].Id, 17, 16, 42),
            new GradeStudentScore(setup.Students[1].Id, 15, 18, 40),
        ], CancellationToken.None);

        var grades = await ReloadGradesAsync(db);
        Assert.Equal(2, grades.Count);
        Assert.All(grades, grade => Assert.Equal("SubmissionRequested", grade.ReviewStatus));

        await service.ReviewAsync(grades[0].Id, "Approved", string.Empty, CancellationToken.None);
        grades = await ReloadGradesAsync(db);
        Assert.All(grades, grade => Assert.Equal("SubmissionAuthorized", grade.ReviewStatus));

        await service.SubmitAuthorizedCourseAsync(grades[0].Id, setup.Teacher.Id, [], CancellationToken.None);
        grades = await ReloadGradesAsync(db);
        Assert.All(grades, grade =>
        {
            Assert.Equal("Submitted", grade.ReviewStatus);
            Assert.NotNull(grade.SubmittedAtUtc);
        });

        await service.ReviewAsync(grades[0].Id, "Approved", string.Empty, CancellationToken.None);
        grades = await ReloadGradesAsync(db);
        Assert.All(grades, grade =>
        {
            Assert.Equal("Approved", grade.ReviewStatus);
            Assert.NotNull(grade.ReviewedAtUtc);
        });
    }

    [Fact]
    public async Task Teacher_resubmission_request_and_corrected_submit_apply_to_complete_course_roster()
    {
        await using var db = CreateContext();
        var setup = await SeedCourseRosterAsync(db);
        var service = new GradeService(db, new InstituteCache());
        await service.RequestCourseSubmissionAsync(setup.Teacher.Id, setup.Course.Id,
        [
            new GradeStudentScore(setup.Students[0].Id, 17, 16, 42),
            new GradeStudentScore(setup.Students[1].Id, 15, 18, 40),
        ], CancellationToken.None);
        var grades = await ReloadGradesAsync(db);
        await service.ReviewAsync(grades[0].Id, "Approved", string.Empty, CancellationToken.None);
        grades = await ReloadGradesAsync(db);
        await service.SubmitAuthorizedCourseAsync(grades[0].Id, setup.Teacher.Id, [], CancellationToken.None);
        grades = await ReloadGradesAsync(db);
        await service.ReviewAsync(grades[0].Id, "Approved", string.Empty, CancellationToken.None);
        grades = await ReloadGradesAsync(db);

        await service.RequestCourseResubmissionAsync(grades[0].Id, setup.Teacher.Id, "Correct the final assessment entries.", CancellationToken.None);
        grades = await ReloadGradesAsync(db);
        Assert.All(grades, grade => Assert.Equal("ResubmitRequested", grade.ReviewStatus));

        await service.ReviewAsync(grades[0].Id, "Approved", string.Empty, CancellationToken.None);
        grades = await ReloadGradesAsync(db);
        Assert.All(grades, grade => Assert.Equal("ResubmitAuthorized", grade.ReviewStatus));

        await service.SubmitAuthorizedCourseAsync(grades[0].Id, setup.Teacher.Id,
        [
            new GradeStudentScore(setup.Students[0].Id, 18, 17, 44),
            new GradeStudentScore(setup.Students[1].Id, 16, 19, 43),
        ], CancellationToken.None);
        grades = await ReloadGradesAsync(db);

        Assert.All(grades, grade =>
        {
            Assert.Equal("Submitted", grade.ReviewStatus);
            Assert.Equal(2, grade.SubmissionVersion);
        });
        Assert.Equal(44, grades.Single(item => item.StudentId == setup.Students[0].Id).FinalExamScore);
        Assert.Equal(43, grades.Single(item => item.StudentId == setup.Students[1].Id).FinalExamScore);
    }

    private static async Task<(Teacher Teacher, Course Course, Student[] Students)> SeedCourseRosterAsync(InstituteDbContext db)
    {
        var department = new Department { DepartmentCode = "DEP-COURSE", Name = "Information Technology" };
        var teacher = new Teacher { TeacherCode = "TEA-COURSE", FullName = "Course Teacher", DepartmentId = department.Id, Department = department };
        var course = new Course { CourseCode = "COU-COURSE", Name = "Software Engineering", DepartmentId = department.Id, Department = department };
        var students = new[]
        {
            new Student { StudentCode = "STU-COURSE-1", FullName = "Student One", DepartmentId = department.Id, Department = department, YearLevel = 2, Shift = "Morning" },
            new Student { StudentCode = "STU-COURSE-2", FullName = "Student Two", DepartmentId = department.Id, Department = department, YearLevel = 2, Shift = "Morning" },
        };
        var schedule = new ScheduleEntry { TimetableCode = "TIM-COURSE", CourseId = course.Id, Course = course, TeacherId = teacher.Id, Teacher = teacher, YearLevel = 2, Shift = "Morning", DayOfWeek = DayOfWeek.Monday, StartsAt = new TimeOnly(8, 0), EndsAt = new TimeOnly(9, 30) };
        db.AddRange(department, teacher, course, schedule);
        db.StudentEnrollments.AddRange(students.Select((student, index) => new StudentEnrollment { EnrollmentCode = $"ENR-1-STU-COURSE-{index + 1}", StudentId = student.Id, Student = student, DepartmentId = department.Id, Department = department, YearLevel = 2, Shift = "Morning", AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active" }));
        db.TimetableEnrollments.Add(new TimetableEnrollment { EnrollmentCode = "ENR-1-TIM-COURSE", ScheduleEntryId = schedule.Id, ScheduleEntry = schedule, CourseId = course.Id, Course = course, TeacherId = teacher.Id, Teacher = teacher, ClassroomId = Guid.NewGuid(), YearLevel = 2, AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active" });
        db.SystemSettings.AddRange(
            new SystemSetting { Section = "academic-year", Key = "currentYear", Value = "2026–2027" },
            new SystemSetting { Section = "semester", Key = "currentTerm", Value = "Semester 1" });
        await db.SaveChangesAsync();
        return (teacher, course, students);
    }

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<List<GradeRecord>> ReloadGradesAsync(InstituteDbContext db)
    {
        db.ChangeTracker.Clear();
        return await db.GradeRecords.OrderBy(item => item.StudentId).ToListAsync();
    }
}
