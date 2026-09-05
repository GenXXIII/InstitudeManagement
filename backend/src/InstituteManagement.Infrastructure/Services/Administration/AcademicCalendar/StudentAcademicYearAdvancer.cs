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
            .Where(student => student.Status != "Inactive" && student.YearLevel >= 1)
            .ToListAsync(cancellationToken);
        var graduates = activeStudents.Where(student => student.YearLevel >= 4).ToList();
        var promoted = activeStudents.Where(student => student.YearLevel < 4).ToList();

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
                Details = $"{student.StudentCode} completed Year 4 and Semester 2 in {oldYear}; removed from current Management and preserved in History."
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
