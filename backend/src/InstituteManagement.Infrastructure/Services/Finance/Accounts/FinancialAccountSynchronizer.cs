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
        var settings = await settingsReader.GetAsync(cancellationToken);
        await UpdatePendingFeesAsync(db.FinancialAccounts
            .Where(account => account.Status == "Pending"
                && !account.DeclaredAtUtc.HasValue
                && account.StudentEnrollment != null
                && account.StudentEnrollment.Status != "Removed"
                && (account.TuitionFee != settings.TuitionFee || account.OtherFee != settings.OtherFee || account.Currency != settings.Currency)),
            settings,
            cancellationToken);
        var enrollments = await db.StudentEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.Student)
            .Where(enrollment => enrollment.Status != "Removed"
                && enrollment.Student != null
                && !db.FinancialAccounts.Any(account => account.StudentEnrollmentId == enrollment.Id))
            .OrderBy(enrollment => enrollment.CreateAt)
            .ToListAsync(cancellationToken);
        return await CreateMissingAsync(enrollments, settings, cancellationToken);
    }

    public async Task<int> EnsurePeriodAsync(
        string academicYear,
        string semester,
        CancellationToken cancellationToken)
    {
        var settings = await settingsReader.GetAsync(cancellationToken);
        await UpdatePendingFeesAsync(db.FinancialAccounts
            .Where(account => account.AcademicYear == academicYear
                && account.Semester == semester
                && account.Status == "Pending"
                && !account.DeclaredAtUtc.HasValue
                && account.StudentEnrollment != null
                && account.StudentEnrollment.Status == "Active"
                && (account.TuitionFee != settings.TuitionFee || account.OtherFee != settings.OtherFee || account.Currency != settings.Currency)),
            settings,
            cancellationToken);
        var enrollments = await db.StudentEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.Student)
            .Where(enrollment =>
                enrollment.AcademicYear == academicYear
                && enrollment.Semester == semester
                && enrollment.Status == "Active"
                && enrollment.Student != null
                && !db.FinancialAccounts.Any(account => account.StudentEnrollmentId == enrollment.Id))
            .OrderBy(enrollment => enrollment.StudentId)
            .ToListAsync(cancellationToken);
        return await CreateMissingAsync(enrollments, settings, cancellationToken);
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

    private async Task<int> CreateMissingAsync(
        IReadOnlyCollection<StudentEnrollment> enrollments,
        FinanceSettings settings,
        CancellationToken cancellationToken)
    {
        var created = 0;
        foreach (var enrollment in enrollments)
        {
            if (db.FinancialAccounts.Local.Any(account => account.StudentEnrollmentId == enrollment.Id)) continue;
            var financeCode = await FinanceCodeAsync(enrollment, enrollment.Student!, cancellationToken);
            db.FinancialAccounts.Add(Create(enrollment, settings, financeCode));
            created++;
        }
        return created;
    }

    private async Task UpdatePendingFeesAsync(
        IQueryable<FinancialAccount> query,
        FinanceSettings settings,
        CancellationToken cancellationToken)
    {
        if (db.Database.IsRelational())
        {
            await query.ExecuteUpdateAsync(update => update
                .SetProperty(account => account.TuitionFee, settings.TuitionFee)
                .SetProperty(account => account.OtherFee, settings.OtherFee)
                .SetProperty(account => account.Currency, settings.Currency), cancellationToken);
            return;
        }

        var accounts = await query.ToListAsync(cancellationToken);
        foreach (var account in accounts) ApplyPendingFees(account, settings);
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
        var code = await BusinessCodeFormatter.GenerateStudentPeriodScopedAsync(
            db,
            enrollment.EnrollmentCode,
            enrollment.YearLevel,
            enrollment.Semester,
            "financeCodePrefix",
            "FIN",
            cancellationToken);
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
