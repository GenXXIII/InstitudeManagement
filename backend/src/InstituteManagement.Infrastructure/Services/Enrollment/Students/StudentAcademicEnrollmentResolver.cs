using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Students;

internal static class StudentAcademicEnrollmentResolver
{
    public static async Task<StudentAcademicEnrollment> GetOrCreateAsync(
        InstituteDbContext db,
        Guid studentId,
        string enrollmentCode,
        CancellationToken cancellationToken)
    {
        var tracked = db.StudentAcademicEnrollments.Local.FirstOrDefault(item =>
            item.EnrollmentCode == enrollmentCode);
        var enrollment = tracked ?? await db.StudentAcademicEnrollments.FirstOrDefaultAsync(
            item => item.EnrollmentCode == enrollmentCode,
            cancellationToken);
        if (enrollment is not null)
        {
            if (enrollment.StudentId != studentId)
                throw new InvalidOperationException("The enrollment code is already owned by another Student.");
            return enrollment;
        }

        enrollment = new StudentAcademicEnrollment
        {
            EnrollmentCode = enrollmentCode,
            StudentId = studentId,
            Status = "Active"
        };
        db.StudentAcademicEnrollments.Add(enrollment);
        return enrollment;
    }

    public static void Complete(StudentAcademicEnrollment? enrollment, DateTime completedAtUtc)
    {
        if (enrollment is null || enrollment.Status == "Completed") return;
        enrollment.Status = "Completed";
        enrollment.CompletedAtUtc = completedAtUtc;
    }
}
