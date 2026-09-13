using InstituteManagement.Application.Features.Grades;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Grades;

public sealed class GradeService(InstituteDbContext db, InstituteCache cache) : IGradeService
{
    public async Task SubmitAsync(Guid studentId, Guid courseId, decimal assignmentScore, decimal midtermScore, decimal finalExamScore, CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty) throw new ArgumentException("StudentId is required.", nameof(studentId));
        if (courseId == Guid.Empty) throw new ArgumentException("CourseId is required.", nameof(courseId));

        var student = await db.Students.FindAsync([studentId], cancellationToken) ?? throw new KeyNotFoundException("Student not found.");
        var course = await db.Courses.FindAsync([courseId], cancellationToken) ?? throw new KeyNotFoundException("Course not found.");
        if (student.Status == "Inactive" || !course.IsActive || student.DepartmentId != course.DepartmentId) throw new InvalidOperationException("Student and course must be active and belong to the same department.");
        var period = await db.SystemSettings.AsNoTracking().Where(x => (x.Section == "academic-year" && x.Key == "currentYear") || (x.Section == "semester" && x.Key == "currentTerm")).ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, cancellationToken);
        var academicYear = period.GetValueOrDefault("academic-year:currentYear", "2026\u20132027");
        var currentTerm = period.GetValueOrDefault("semester:currentTerm", "Semester 1");
        var grade = await db.GradeRecords.FirstOrDefaultAsync(x => x.StudentId == studentId && x.CourseId == courseId && x.AcademicYear == academicYear && x.Term == currentTerm, cancellationToken);
        if (grade is null) { grade = new GradeRecord { GradeCode = await BusinessCodeFormatter.GenerateAsync(db, "grade", cancellationToken), StudentId = studentId, CourseId = courseId, AcademicYear = academicYear, Term = currentTerm }; db.GradeRecords.Add(grade); }
        var rules = await GradeCompositionCalculator.LoadRulesAsync(db, cancellationToken);
        var attendance = await GradeCompositionCalculator.AttendanceAsync(db, studentId, courseId, academicYear, currentTerm, rules.Weights.Attendance, cancellationToken);
        GradeCompositionCalculator.Apply(grade, rules.Weights, rules.Thresholds, attendance, assignmentScore, midtermScore, finalExamScore);
        grade.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { ResourceId = grade.Id, Type = "Grade", Subject = student.FullName, Action = $"Grade {grade.LetterGrade}", Details = $"{course.Name}: attendance {grade.AttendanceScore:0.##}/{grade.AttendanceMaximum:0.##}, assignment {grade.AssignmentScore:0.##}/{grade.AssignmentMaximum:0.##}, midterm {grade.MidtermScore:0.##}/{grade.MidtermMaximum:0.##}, final {grade.FinalExamScore:0.##}/{grade.FinalExamMaximum:0.##}, total {grade.Score:0.##}/100 · {academicYear} · {currentTerm}" });
        var reminders = await db.SystemSettings.AsNoTracking().Where(x => x.Section == "notifications" && x.Key == "gradeReminders").Select(x => x.Value).FirstOrDefaultAsync(cancellationToken);
        if (grade.LetterGrade is "E" or "F" && (!bool.TryParse(reminders, out var enabled) || enabled)) db.Notifications.Add(new Notification { Title = "Grade support reminder", Message = $"{student.FullName} received {grade.LetterGrade} in {course.Name}.", Severity = grade.LetterGrade == "F" ? "Warning" : "Info" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }
}
