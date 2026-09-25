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
    public async Task Current_semester_shows_draft_course_cards_until_student_results_are_confirmed()
    {
        await using var db = new InstituteDbContext(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var department = new Department { DepartmentCode = "DEP-DRAFT", Name = "Draft Results" };
        var teacher = new Teacher { TeacherCode = "TEA-DRAFT", FullName = "Draft Teacher", DepartmentId = department.Id, Department = department };
        var student = new Student { StudentCode = "STU-DRAFT", FullName = "Draft Student", DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning" };
        var enrollment = new StudentEnrollment { EnrollmentCode = "ENR-DRAFT", StudentId = student.Id, Student = student, DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning", AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active" };
        db.AddRange(department, teacher, student, enrollment,
            new SystemSetting { Section = "academic-year", Key = "currentYear", Value = "2026–2027" },
            new SystemSetting { Section = "semester", Key = "currentTerm", Value = "Semester 1" },
            new SystemSetting { Section = "grade-rules", Key = "expectedCourseCount", Value = "5" });
        for (var index = 1; index <= 5; index++)
        {
            var course = new Course { CourseCode = $"COU-DRAFT-{index}", Name = $"Draft Course {index}", YearLevel = 1, Semester = "Semester 1", DepartmentId = department.Id, Department = department };
            var schedule = new ScheduleEntry { TimetableCode = $"TIM-DRAFT-{index}", CourseId = course.Id, Course = course, TeacherId = teacher.Id, Teacher = teacher, YearLevel = 1, Shift = "Morning", DayOfWeek = DayOfWeek.Monday, StartsAt = new TimeOnly(7, 30), EndsAt = new TimeOnly(9, 0) };
            var timetable = new TimetableEnrollment { EnrollmentCode = $"ENR-TIM-DRAFT-{index}", ScheduleEntryId = schedule.Id, ScheduleEntry = schedule, CourseId = course.Id, Course = course, TeacherId = teacher.Id, Teacher = teacher, ClassroomId = Guid.NewGuid(), YearLevel = 1, AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active" };
            db.AddRange(course, schedule, timetable);
        }
        await db.SaveChangesAsync();
        var service = new ResultQueryService(db, new FinancialProgression(db, new ActivePeriodLedgerCreator(db)));

        var draft = Assert.Single(await service.GetAsync(null, null, null, null, false, CancellationToken.None));

        Assert.Equal("Draft", draft.PublicationStatus);
        Assert.Equal(5, draft.Grades.Count);
        Assert.All(draft.Grades, course =>
        {
            Assert.Equal("Draft", course.Grade);
            Assert.Null(course.Score);
            Assert.False(course.IsApproved);
        });
        var incomplete = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishAllAsync(CancellationToken.None));
        Assert.Contains("Every current student", incomplete.Message);

        foreach (var course in db.Courses)
            db.GradeRecords.Add(new GradeRecord { GradeCode = $"GRD-{course.CourseCode}", StudentId = student.Id, CourseId = course.Id, AcademicYear = "2026–2027", Term = "Semester 1", Score = 84, LetterGrade = "B", ReviewStatus = "Approved", SubmittedByTeacherId = teacher.Id, SubmittedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var approvedButUnconfirmed = Assert.Single(await service.GetAsync(null, null, null, null, false, CancellationToken.None));
        Assert.Equal("Draft", approvedButUnconfirmed.PublicationStatus);
        Assert.All(approvedButUnconfirmed.Grades, course => Assert.Equal("Draft", course.Grade));

        foreach (var grade in db.GradeRecords) grade.FinalizedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var confirmed = Assert.Single(await service.GetAsync(null, null, null, null, false, CancellationToken.None));
        Assert.Equal("Ready", confirmed.PublicationStatus);
        Assert.All(confirmed.Grades, course =>
        {
            Assert.Equal(84, course.Score);
            Assert.Equal("B", course.Grade);
            Assert.True(course.IsApproved);
        });
    }

    [Fact]
    public async Task Publish_all_keeps_current_results_read_only_then_archives_them_after_payment_closure_and_semester_end()
    {
        await using var db = new InstituteDbContext(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var department = new Department { DepartmentCode = "DEP-R", Name = "Results" };
        var teacher = new Teacher { TeacherCode = "TEA-R", FullName = "Teacher Results" };
        var student = new Student { StudentCode = "STU-R", FullName = "Student Results", DepartmentId = department.Id, Department = department, YearLevel = 2, Shift = "Morning" };
        var enrollment = new StudentEnrollment { EnrollmentCode = "STU-R-ESTU-1", StudentId = student.Id, Student = student, DepartmentId = department.Id, Department = department, YearLevel = 2, Shift = "Evening", AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active" };
        var payment = new FinancialAccount { FinancialAccountCode = "FIN-R", StudentEnrollmentId = enrollment.Id, StudentEnrollment = enrollment, StudentId = student.Id, Student = student, AcademicYear = "2026–2027", Semester = "Semester 1", TuitionFee = 500, Status = "Paid", ClosedAtUtc = DateTime.UtcNow };
        db.AddRange(department, teacher, student, enrollment, payment,
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

        Assert.Equal(1, await service.PublishAllAsync(CancellationToken.None));
        var declared = Assert.Single(await service.GetAsync(null, null, null, null, false, CancellationToken.None));
        Assert.Equal("Published", declared.PublicationStatus);
        Assert.Empty(await service.GetAsync(null, null, null, null, true, student.Id, true, CancellationToken.None));
        db.SystemSettings.Single(item => item.Section == "semester" && item.Key == "currentTerm").Value = "Semester 2";
        await db.SaveChangesAsync();
        var history = await service.GetAsync(null, null, null, null, true, student.Id, true, CancellationToken.None);

        var result = Assert.Single(history);
        Assert.True(result.IsPublished);
        Assert.Equal("Published", result.PublicationStatus);
        Assert.Equal(5, result.TotalCourses);
        Assert.Equal(400, result.TotalScore);
    }
}
