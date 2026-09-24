using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Results;

public sealed record SemesterResultDeclarationGateResult(IReadOnlySet<Guid> DeclaredStudentIds, int HeldStudents);

public sealed class SemesterResultDeclarationGate(InstituteDbContext db)
{
    public async Task<SemesterResultDeclarationGateResult> EvaluateAsync(
        string academicYear,
        string term,
        CancellationToken cancellationToken)
    {
        var students = await db.StudentEnrollments.AsNoTracking()
            .Include(item => item.Student)
            .Where(item => item.AcademicYear == academicYear
                && item.Semester == term
                && item.Status == "Active"
                && item.Student != null
                && item.Student.Status != "Inactive")
            .Select(item => new { item.StudentId, item.Student!.FullName })
            .ToListAsync(cancellationToken);
        var declared = (await db.SemesterResultPublications.AsNoTracking()
            .Where(item => item.AcademicYear == academicYear && item.Term == term)
            .Select(item => item.StudentId)
            .ToListAsync(cancellationToken))
            .ToHashSet();
        var held = students.Where(item => !declared.Contains(item.StudentId)).ToList();
        foreach (var student in held)
        {
            db.Notifications.Add(new Notification
            {
                Title = "Semester result declaration required",
                Message = $"{student.FullName} remains in {academicYear} Â· {term} until the Administrator declares the Semester Result.",
                Severity = "Warning"
            });
        }
        return new SemesterResultDeclarationGateResult(declared, held.Count);
    }
}
