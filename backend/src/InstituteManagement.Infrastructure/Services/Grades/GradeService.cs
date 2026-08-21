using InstituteManagement.Application.Abstractions;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Grades;

public sealed class GradeService(InstituteDbContext db, InstituteCache cache) : IGradeService
{
    public async Task SubmitAsync(Guid studentId, Guid courseId, decimal score, CancellationToken cancellationToken)
    {
        var student = await db.Students.FindAsync([studentId], cancellationToken) ?? throw new KeyNotFoundException("Student not found.");
        var course = await db.Courses.FindAsync([courseId], cancellationToken) ?? throw new KeyNotFoundException("Course not found.");
        if (student.Status == "Inactive" || !course.IsActive || student.DepartmentId != course.DepartmentId) throw new InvalidOperationException("Student and course must be active and belong to the same department.");
        var grade = await db.GradeRecords.FirstOrDefaultAsync(x => x.StudentId == studentId && x.CourseId == courseId, cancellationToken);
        if (grade is null) { grade = new GradeRecord { StudentId = studentId, CourseId = courseId }; db.GradeRecords.Add(grade); }
        grade.Score = score; grade.LetterGrade = await LetterAsync(score, cancellationToken); grade.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { ResourceId = grade.Id, Type = "Grade", Subject = student.FullName, Action = $"Grade {grade.LetterGrade}", Details = $"{course.Name}: {score:0.0}" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync();
    }

    private async Task<string> LetterAsync(decimal score, CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.AsNoTracking().Where(x => x.Section == "grade-rules").ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        decimal Rule(string key, decimal fallback) => decimal.TryParse(settings.GetValueOrDefault(key), out var value) ? value : fallback;
        return score >= Rule("aMinimum", 90) ? "A" : score >= Rule("bMinimum", 80) ? "B" : score >= Rule("cMinimum", 70) ? "C" : score >= Rule("dMinimum", 60) ? "D" : "F";
    }
}
