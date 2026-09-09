using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed record AcademicPeriodEnrollmentAdvanceResult(int StudentsEnrolled);

public sealed class AcademicPeriodEnrollmentAdvancer(InstituteDbContext db)
{
    public async Task<AcademicPeriodEnrollmentAdvanceResult> AdvanceAsync(
        string previousAcademicYear,
        string previousSemester,
        string nextAcademicYear,
        string nextSemester,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(previousAcademicYear)
            || string.IsNullOrWhiteSpace(previousSemester)
            || previousAcademicYear == nextAcademicYear && previousSemester == nextSemester)
        {
            return new(0);
        }

        var previousEnrollments = await db.StudentEnrollments
            .AsNoTracking()
            .Where(enrollment =>
                enrollment.AcademicYear == previousAcademicYear
                && enrollment.Semester == previousSemester
                && enrollment.Status == "Active")
            .OrderBy(enrollment => enrollment.StudentId)
            .ToListAsync(cancellationToken);
        if (previousEnrollments.Count == 0) return new(0);

        var studentIds = previousEnrollments.Select(enrollment => enrollment.StudentId).Distinct().ToList();
        var students = (await db.Students
                .AsNoTracking()
                .Where(student => studentIds.Contains(student.Id))
                .ToListAsync(cancellationToken))
            .ToDictionary(student => student.Id);
        foreach (var tracked in db.ChangeTracker.Entries<Student>()
                     .Where(entry => studentIds.Contains(entry.Entity.Id) && entry.State is not EntityState.Deleted and not EntityState.Detached))
        {
            students[tracked.Entity.Id] = tracked.Entity;
        }

        var alreadyEnrolled = (await db.StudentEnrollments
                .AsNoTracking()
                .Where(enrollment =>
                    studentIds.Contains(enrollment.StudentId)
                    && enrollment.AcademicYear == nextAcademicYear
                    && enrollment.Semester == nextSemester)
                .Select(enrollment => enrollment.StudentId)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        alreadyEnrolled.UnionWith(db.StudentEnrollments.Local
            .Where(enrollment =>
                enrollment.AcademicYear == nextAcademicYear
                && enrollment.Semester == nextSemester)
            .Select(enrollment => enrollment.StudentId));

        var created = 0;
        foreach (var previous in previousEnrollments)
        {
            if (alreadyEnrolled.Contains(previous.StudentId)
                || !students.TryGetValue(previous.StudentId, out var student)
                || student.Status == "Inactive"
                || student.YearLevel is < 1 or > 4)
            {
                continue;
            }

            db.StudentEnrollments.Add(new StudentEnrollment
            {
                EnrollmentCode = previous.EnrollmentCode,
                StudentId = previous.StudentId,
                DepartmentId = previous.DepartmentId,
                YearLevel = student.YearLevel,
                Shift = previous.Shift,
                AcademicYear = nextAcademicYear,
                Semester = nextSemester,
                Status = "Active"
            });
            alreadyEnrolled.Add(previous.StudentId);
            created++;
        }

        return new(created);
    }
}
