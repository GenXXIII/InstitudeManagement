using System.Text.Json;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed record StudentYearAdvanceResult(int Promoted, int Graduated);

public sealed class StudentAcademicYearAdvancer(InstituteDbContext db)
{
    public async Task<StudentYearAdvanceResult> AdvanceAsync(string oldYear, CancellationToken cancellationToken)
    {
        var activeStudents = await db.Students
            .Include(student => student.Department)
            .Where(student => student.Status != "Inactive" && student.YearLevel >= 1)
            .ToListAsync(cancellationToken);
        var graduates = activeStudents.Where(student => student.YearLevel >= 4).ToList();
        var promoted = activeStudents.Where(student => student.YearLevel < 4).ToList();
        var graduateIds = graduates.Select(student => student.Id).ToList();
        var finalEnrollments = await db.StudentEnrollments.AsNoTracking()
            .Include(enrollment => enrollment.Department)
            .Where(enrollment => graduateIds.Contains(enrollment.StudentId) && enrollment.AcademicYear == oldYear)
            .ToListAsync(cancellationToken);

        foreach (var student in graduates)
        {
            student.Status = "Inactive";
            student.UpdatedAtUtc = DateTime.UtcNow;
            db.AuditLogs.Add(new AuditLog
            {
                ResourceId = student.Id,
                Type = "Student",
                Subject = student.FullName,
                Action = "Graduated",
                Details = JsonSerializer.Serialize(new
                {
                    studentCode = student.StudentCode,
                    name = student.FullName,
                    student.Email,
                    student.DepartmentId,
                    department = student.Department?.Name,
                    completedYear = 4,
                    graduationAcademicYear = oldYear,
                    status = "Graduated",
                    archive = "Management profile and the complete Enrollment, Operation, and Record story are preserved in History.",
                    finalEnrollments = finalEnrollments
                        .Where(enrollment => enrollment.StudentId == student.Id)
                        .OrderBy(enrollment => enrollment.Semester)
                        .Select(enrollment => new
                        {
                            enrollment.EnrollmentCode,
                            enrollment.AcademicYear,
                            enrollment.Semester,
                            enrollment.YearLevel,
                            enrollment.Shift,
                            department = enrollment.Department?.Name,
                            enrollment.Status
                        })
                })
            });
        }

        foreach (var student in promoted)
        {
            student.YearLevel++;
            student.UpdatedAtUtc = DateTime.UtcNow;
        }

        db.AuditLogs.Add(new AuditLog
        {
            Type = "Academic calendar",
            Subject = oldYear,
            Action = "Year rollover",
            Details = $"Closed {oldYear}; promoted {promoted.Count} active Year 1-3 students and graduated {graduates.Count} Year 4 students. Grade, attendance, and completed-class rows remain in history."
        });
        return new(promoted.Count, graduates.Count);
    }
}
