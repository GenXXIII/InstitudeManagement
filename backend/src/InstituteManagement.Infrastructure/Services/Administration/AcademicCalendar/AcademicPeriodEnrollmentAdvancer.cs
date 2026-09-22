using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
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
        IReadOnlySet<Guid> paidStudentIds,
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
                && paidStudentIds.Contains(enrollment.StudentId)
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

            var codes = await BusinessCodeFormatter.GenerateEnrollmentWorkflowAsync(db, student.StudentCode, "student", student.Id, cancellationToken);
            var enrollmentId = Guid.NewGuid();
            var nextEnrollment = new StudentEnrollment
            {
                Id = enrollmentId,
                EnrollmentCode = codes.Enrollment,
                PublicId = await BusinessCodeFormatter.GenerateEnrollmentPublicIdAsync(db, enrollmentId, "studentPublicIdPrefix", "STU", cancellationToken),
                FinanceCode = await BusinessCodeFormatter.GenerateEnrollmentScopedAsync(db, student.StudentCode, "student", codes.Enrollment, "financeCodePrefix", "FIN", cancellationToken),
                ResultCode = await BusinessCodeFormatter.GenerateEnrollmentScopedAsync(db, student.StudentCode, "student", codes.Enrollment, "resultCodePrefix", "RES", cancellationToken),
                OperationCode = codes.Operation,
                RecordCode = codes.Record,
                HistoryCode = codes.History,
                StudentId = previous.StudentId,
                DepartmentId = previous.DepartmentId,
                YearLevel = student.YearLevel,
                Shift = previous.Shift,
                AcademicYear = nextAcademicYear,
                Semester = nextSemester,
                Status = "Active"
            };
            db.StudentEnrollments.Add(nextEnrollment);
            alreadyEnrolled.Add(previous.StudentId);
            created++;
        }

        return new(created);
    }
}
