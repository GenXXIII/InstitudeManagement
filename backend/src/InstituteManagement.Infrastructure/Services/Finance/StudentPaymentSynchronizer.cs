using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
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
        var annualCoverage = await FindAnnualCoverageAsync(enrollment, cancellationToken);
        if (annualCoverage is not null) return annualCoverage;
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

        var account = Create(enrollment, student, settings);
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
        var annualCoverageKeys = (await db.FinancialAccounts.AsNoTracking()
                .Where(account =>
                    account.Semester == "Semester 1"
                    && account.PaymentPlan == "Year"
                    && account.DeclaredAtUtc != null
                    && account.Status == "Paid")
                .Select(account => new { account.StudentId, account.AcademicYear })
                .ToListAsync(cancellationToken))
            .Select(account => AnnualCoverageKey(account.StudentId, account.AcademicYear))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var localAnnual in db.FinancialAccounts.Local.Where(account =>
                     account.Semester == "Semester 1"
                     && account.PaymentPlan == "Year"
                     && account.DeclaredAtUtc != null
                     && account.Status == "Paid"))
            annualCoverageKeys.Add(AnnualCoverageKey(localAnnual.StudentId, localAnnual.AcademicYear));
        var created = 0;
        foreach (var enrollment in enrollments)
        {
            if (enrollment.Semester == "Semester 2" && annualCoverageKeys.Contains(AnnualCoverageKey(enrollment.StudentId, enrollment.AcademicYear)))
                continue;
            if (existing.TryGetValue(enrollment.Id, out var account))
            {
                ApplyPendingFees(account, settings);
                continue;
            }

            db.FinancialAccounts.Add(Create(enrollment, enrollment.Student!, settings));
            created++;
        }
        return created;
    }

    private async Task<FinancialAccount?> FindAnnualCoverageAsync(
        StudentEnrollment enrollment,
        CancellationToken cancellationToken)
    {
        if (enrollment.Semester != "Semester 2") return null;
        var local = db.FinancialAccounts.Local.FirstOrDefault(account =>
            account.StudentId == enrollment.StudentId
            && account.AcademicYear == enrollment.AcademicYear
            && account.Semester == "Semester 1"
            && account.PaymentPlan == "Year"
            && account.DeclaredAtUtc != null
            && account.Status == "Paid");
        return local ?? await db.FinancialAccounts.FirstOrDefaultAsync(account =>
            account.StudentId == enrollment.StudentId
            && account.AcademicYear == enrollment.AcademicYear
            && account.Semester == "Semester 1"
            && account.PaymentPlan == "Year"
            && account.DeclaredAtUtc != null
            && account.Status == "Paid", cancellationToken);
    }

    private static FinancialAccount Create(
        StudentEnrollment enrollment,
        Student student,
        FinanceSettings settings)
    {
        return new FinancialAccount
        {
            FinancialAccountCode = FinancialAccountCode(student.StudentCode),
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

    private static void ApplyPendingFees(FinancialAccount account, FinanceSettings settings)
    {
        if (account.Status != "Pending" || account.DeclaredAtUtc.HasValue) return;
        account.TuitionFee = settings.TuitionFee;
        account.OtherFee = settings.OtherFee;
        account.Currency = settings.Currency;
    }

    private static string FinancialAccountCode(string studentCode)
    {
        var safeCode = studentCode.Trim();
        if (safeCode.Length > 52) safeCode = safeCode[..52];
        return $"FIN-{safeCode}";
    }

    private static string AnnualCoverageKey(Guid studentId, string academicYear) => $"{studentId:N}|{academicYear}";
}
