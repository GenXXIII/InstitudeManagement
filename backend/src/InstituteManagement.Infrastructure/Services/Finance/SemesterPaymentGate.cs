using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed record SemesterPaymentGateResult(IReadOnlySet<Guid> PaidStudentIds, int HeldStudents);

public sealed class SemesterPaymentGate(
    InstituteDbContext db,
    FinancialAccountSynchronizer synchronizer,
    FinanceSettingsReader settingsReader)
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
        var accounts = await db.FinancialAccounts
            .Where(account => enrollmentIds.Contains(account.StudentEnrollmentId))
            .ToListAsync(cancellationToken);
        foreach (var tracked in db.FinancialAccounts.Local.Where(account => enrollmentIds.Contains(account.StudentEnrollmentId)))
        {
            if (accounts.All(account => account.Id != tracked.Id)) accounts.Add(tracked);
        }

        var settings = await settingsReader.GetAsync(cancellationToken);
        var paid = accounts
            .Where(account => !settings.RequirePaidForAdvancement || account.Status == "Paid")
            .Select(account => account.StudentId)
            .ToHashSet();
        var reminderTime = DateTime.UtcNow;
        var held = 0;
        foreach (var account in accounts.Where(account => settings.RequirePaidForAdvancement && account.Status != "Paid"))
        {
            account.ReminderSentAtUtc = reminderTime;
            account.ReminderReadAtUtc = null;
            account.UpdatedAtUtc = reminderTime;
            held++;
        }

        return new(paid, held);
    }
}
