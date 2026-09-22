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

    [Fact]
    public async Task Rejected_submission_requires_administrator_permission_before_teacher_can_resubmit()
    {
        await using var db = CreateContext();
        var department = new Department { DepartmentCode = "DEP-2", Name = "Business" };
        var student = new Student { StudentCode = "STU-2", FullName = "Student Two", DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning" };
        var course = new Course { CourseCode = "COU-2", Name = "Accounting", DepartmentId = department.Id, Department = department };
        var teacher = new Teacher { TeacherCode = "TEA-2", FullName = "Teacher Two", DepartmentId = department.Id, Department = department };
        db.AddRange(department, student, course, teacher);
        AddSetting(db, "academic-year", "currentYear", "2026–2027");
        AddSetting(db, "semester", "currentTerm", "Semester 1");
        await db.SaveChangesAsync();
        var service = new GradeService(db, new InstituteCache());

        await service.SubmitAsync(student.Id, course.Id, 12, 14, 40, CancellationToken.None);
        var grade = Assert.Single(db.GradeRecords);
        grade.SubmittedByTeacherId = teacher.Id;
        await db.SaveChangesAsync();
        await service.ReviewAsync(grade.Id, "Rejected", "Correct the Midterm score.", CancellationToken.None);
        await service.RequestResubmissionAsync(grade.Id, teacher.Id, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(student.Id, course.Id, 12, 16, 40, CancellationToken.None));
        await service.AuthorizeResubmissionAsync(grade.Id, CancellationToken.None);
        await service.SubmitAsync(student.Id, course.Id, 12, 16, 40, CancellationToken.None);

        Assert.Equal("Pending", grade.ReviewStatus);
        Assert.Equal(2, grade.SubmissionVersion);
        Assert.Equal(16, grade.MidtermScore);
        Assert.Equal(string.Empty, grade.ReviewNote);
    }

    [Fact]
    public async Task Teacher_can_replace_imported_grade_with_first_reviewed_submission()
    {
        await using var db = CreateContext();
        var department = new Department { DepartmentCode = "DEP-3", Name = "Technology" };
        var student = new Student { StudentCode = "STU-3", FullName = "Student Three", DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning" };
        var course = new Course { CourseCode = "COU-3", Name = "Networks", DepartmentId = department.Id, Department = department };
        var teacher = new Teacher { TeacherCode = "TEA-3", FullName = "Teacher Three", DepartmentId = department.Id, Department = department };
        var enrollment = new StudentEnrollment { EnrollmentCode = "ENR-1-STU-3", StudentId = student.Id, Student = student, DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning", AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active" };
        var schedule = new ScheduleEntry { TimetableCode = "TIM-3", CourseId = course.Id, Course = course, TeacherId = teacher.Id, Teacher = teacher, YearLevel = 1, Shift = "Morning", DayOfWeek = DayOfWeek.Monday, StartsAt = new TimeOnly(7, 30), EndsAt = new TimeOnly(9, 0) };
        var assignment = new TimetableEnrollment { EnrollmentCode = "ENR-1-TIM-3", ScheduleEntryId = schedule.Id, ScheduleEntry = schedule, CourseId = course.Id, Course = course, TeacherId = teacher.Id, Teacher = teacher, ClassroomId = Guid.NewGuid(), YearLevel = 1, AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active" };
        var imported = new GradeRecord { GradeCode = "GRD-3", StudentId = student.Id, Student = student, CourseId = course.Id, Course = course, AcademicYear = "2026–2027", Term = "Semester 1", AssignmentScore = 1, MidtermScore = 1, FinalExamScore = 1, ReviewStatus = "Approved", SubmissionVersion = 1 };
        db.AddRange(department, student, course, teacher, enrollment, schedule, assignment, imported);
        AddSetting(db, "academic-year", "currentYear", "2026–2027");
        AddSetting(db, "semester", "currentTerm", "Semester 1");
        await db.SaveChangesAsync();

        await new GradeService(db, new InstituteCache()).SubmitAsync(student.Id, course.Id, teacher.Id, 15, 16, 42, CancellationToken.None);

        Assert.Equal(teacher.Id, imported.SubmittedByTeacherId);
        Assert.Equal("Pending", imported.ReviewStatus);
        Assert.Equal(1, imported.SubmissionVersion);
        Assert.Equal(15, imported.AssignmentScore);
        Assert.Equal(16, imported.MidtermScore);
        Assert.Equal(42, imported.FinalExamScore);
    }

    [Fact]
    public async Task Confirmed_teacher_submission_can_request_permission_for_a_new_submit()
    {
        await using var db = CreateContext();
        var department = new Department { DepartmentCode = "DEP-4", Name = "Design" };
        var student = new Student { StudentCode = "STU-4", FullName = "Student Four", DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning" };
        var course = new Course { CourseCode = "COU-4", Name = "Drawing", DepartmentId = department.Id, Department = department };
        var teacher = new Teacher { TeacherCode = "TEA-4", FullName = "Teacher Four", DepartmentId = department.Id, Department = department };
        db.AddRange(department, student, course, teacher);
        AddSetting(db, "academic-year", "currentYear", "2026–2027");
        AddSetting(db, "semester", "currentTerm", "Semester 1");
        await db.SaveChangesAsync();
        var service = new GradeService(db, new InstituteCache());

        await service.SubmitAsync(student.Id, course.Id, 12, 14, 40, CancellationToken.None);
        var grade = Assert.Single(db.GradeRecords);
        grade.SubmittedByTeacherId = teacher.Id;
        await db.SaveChangesAsync();
        await service.ReviewAsync(grade.Id, "Approved", string.Empty, CancellationToken.None);
        await service.RequestResubmissionAsync(grade.Id, teacher.Id, CancellationToken.None);

        Assert.Equal("ResubmitRequested", grade.ReviewStatus);
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
