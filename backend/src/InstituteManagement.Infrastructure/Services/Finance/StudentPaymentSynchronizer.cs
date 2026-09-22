using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed class FinancialAccountSynchronizer(
    InstituteDbContext db,
    FinanceSettingsReader settingsReader)
{
    public async Task<int> EnsureAllAsync(CancellationToken cancellationToken)
    {
        var enrollments = await db.StudentEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.Student)
            .Where(enrollment => enrollment.Status != "Removed" && enrollment.Student != null)
            .OrderBy(enrollment => enrollment.CreateAt)
            .ToListAsync(cancellationToken);
        return await EnsureAsync(enrollments, cancellationToken);
    }

    public async Task<int> EnsurePeriodAsync(
        string academicYear,
        string semester,
        CancellationToken cancellationToken)
    {
        var enrollments = await db.StudentEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.Student)
            .Where(enrollment =>
                enrollment.AcademicYear == academicYear
                && enrollment.Semester == semester
                && enrollment.Status == "Active"
                && enrollment.Student != null)
            .OrderBy(enrollment => enrollment.StudentId)
            .ToListAsync(cancellationToken);
        return await EnsureAsync(enrollments, cancellationToken);
    }

    public async Task<FinancialAccount> EnsureForEnrollmentAsync(
        StudentEnrollment enrollment,
        Student student,
        CancellationToken cancellationToken)
    {
        var local = db.FinancialAccounts.Local.FirstOrDefault(account => account.StudentEnrollmentId == enrollment.Id);
        var existing = local ?? await db.FinancialAccounts.FirstOrDefaultAsync(
            account => account.StudentEnrollmentId == enrollment.Id,
            cancellationToken);
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (existing is not null)
        {
            ApplyPendingFees(existing, settings);
            return existing;
        }

        var financeCode = await FinanceCodeAsync(enrollment, student, cancellationToken);
        var account = Create(enrollment, settings, financeCode);
        db.FinancialAccounts.Add(account);
        return account;
    }

    private async Task<int> EnsureAsync(
        IReadOnlyCollection<StudentEnrollment> enrollments,
        CancellationToken cancellationToken)
    {
        if (enrollments.Count == 0) return 0;
        var enrollmentIds = enrollments.Select(enrollment => enrollment.Id).ToList();
        var existing = (await db.FinancialAccounts
                .Where(account => enrollmentIds.Contains(account.StudentEnrollmentId))
                .ToListAsync(cancellationToken))
            .ToDictionary(account => account.StudentEnrollmentId);
        foreach (var local in db.FinancialAccounts.Local.Where(account => enrollmentIds.Contains(account.StudentEnrollmentId)))
        {
            existing[local.StudentEnrollmentId] = local;
        }

        var settings = await settingsReader.GetAsync(cancellationToken);
        var created = 0;
        foreach (var enrollment in enrollments)
        {
            if (existing.TryGetValue(enrollment.Id, out var account))
            {
                ApplyPendingFees(account, settings);
                continue;
            }

            var financeCode = await FinanceCodeAsync(enrollment, enrollment.Student!, cancellationToken);
            db.FinancialAccounts.Add(Create(enrollment, settings, financeCode));
            created++;
        }
        return created;
    }

    private static FinancialAccount Create(
        StudentEnrollment enrollment,
        FinanceSettings settings,
        string financeCode)
    {
        return new FinancialAccount
        {
            FinancialAccountCode = financeCode,
            StudentEnrollmentId = enrollment.Id,
            StudentId = enrollment.StudentId,
            AcademicYear = enrollment.AcademicYear,
            Semester = enrollment.Semester,
            TuitionFee = settings.TuitionFee,
            OtherFee = settings.OtherFee,
            Currency = settings.Currency,
            DueOn = DateOnly.FromDateTime(enrollment.CreateAt).AddDays(settings.PaymentDueDays),
            Status = "Pending"
        };
    }

    private async Task<string> FinanceCodeAsync(StudentEnrollment enrollment, Student student, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(enrollment.FinanceCode)) return enrollment.FinanceCode;
        var code = await BusinessCodeFormatter.GenerateEnrollmentScopedAsync(db, student.StudentCode, "student", enrollment.EnrollmentCode, "financeCodePrefix", "FIN", cancellationToken);
        if (db.Entry(enrollment).State != EntityState.Detached) enrollment.FinanceCode = code;
        return code;
    }

    private static void ApplyPendingFees(FinancialAccount account, FinanceSettings settings)
    {
        if (account.Status != "Pending" || account.DeclaredAtUtc.HasValue) return;
        account.TuitionFee = settings.TuitionFee;
        account.OtherFee = settings.OtherFee;
        account.Currency = settings.Currency;
    }

}
