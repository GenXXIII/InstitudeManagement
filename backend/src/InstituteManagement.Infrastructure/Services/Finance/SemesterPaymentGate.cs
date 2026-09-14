using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed record SemesterPaymentGateResult(IReadOnlySet<Guid> PaidStudentIds, int HeldStudents);

public sealed class SemesterPaymentGate(
    InstituteDbContext db,
    StudentPaymentSynchronizer synchronizer)
{
    public async Task<SemesterPaymentGateResult> EvaluateAsync(
        string academicYear,
        string semester,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(academicYear) || string.IsNullOrWhiteSpace(semester))
            return new(new HashSet<Guid>(), 0);

        await synchronizer.EnsurePeriodAsync(academicYear, semester, cancellationToken);
        var enrollmentIds = await db.StudentEnrollments.AsNoTracking()
            .Where(enrollment =>
                enrollment.AcademicYear == academicYear
                && enrollment.Semester == semester
                && enrollment.Status == "Active")
            .Select(enrollment => enrollment.Id)
            .ToListAsync(cancellationToken);
        var payments = await db.StudentPayments
            .Where(payment => enrollmentIds.Contains(payment.StudentEnrollmentId))
            .ToListAsync(cancellationToken);
        foreach (var tracked in db.StudentPayments.Local.Where(payment => enrollmentIds.Contains(payment.StudentEnrollmentId)))
        {
            if (payments.All(payment => payment.Id != tracked.Id)) payments.Add(tracked);
        }

        var paid = payments
            .Where(payment => payment.Status == "Paid")
            .Select(payment => payment.StudentId)
            .ToHashSet();
        var reminderTime = DateTime.UtcNow;
        var held = 0;
        foreach (var payment in payments.Where(payment => payment.Status != "Paid"))
        {
            payment.ReminderSentAtUtc = reminderTime;
            payment.ReminderReadAtUtc = null;
            payment.UpdatedAtUtc = reminderTime;
            held++;
        }

        return new(paid, held);
    }
}
