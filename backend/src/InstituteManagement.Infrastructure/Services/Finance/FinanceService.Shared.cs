using System.Text.Json;
using InstituteManagement.Application.Features.Finance;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed partial class FinanceService
{
    private IQueryable<FinancialAccount> AccountQuery() => db.FinancialAccounts
        .Include(account => account.Payments)
        .Include(account => account.Student)
        .Include(account => account.StudentEnrollment)!
            .ThenInclude(enrollment => enrollment!.Department);

    private async Task<FinancialAccount> FindAccountAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await AccountQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Financial account not found.");
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (ApplyLatePenalty(account, settings)) await SaveAsync(cancellationToken);
        return account;
    }

    private async Task<FinancialAccount> FindStudentAccountAsync(Guid studentId, Guid paymentId, CancellationToken cancellationToken)
    {
        var account = await AccountQuery().SingleOrDefaultAsync(
            item => item.Id == paymentId && item.StudentId == studentId,
            cancellationToken) ?? throw new KeyNotFoundException("Student financial account not found.");
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (ApplyLatePenalty(account, settings)) await SaveAsync(cancellationToken);
        return account;
    }

    private async Task<List<FinancialAccount>> CurrentActiveAccountsAsync(
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken)
    {
        var period = await db.SystemSettings.AsNoTracking()
            .Where(setting =>
                setting.Section == "academic-year" && setting.Key == "currentYear"
                || setting.Section == "semester" && setting.Key == "currentTerm")
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, cancellationToken);
        var academicYear = period.GetValueOrDefault("academic-year:currentYear", string.Empty);
        var semester = period.GetValueOrDefault("semester:currentTerm", string.Empty);
        if (string.IsNullOrWhiteSpace(academicYear) || string.IsNullOrWhiteSpace(semester))
            throw new InvalidOperationException("Set the current academic year and semester before closing payments.");

        await synchronizer.EnsurePeriodAsync(academicYear, semester, cancellationToken);
        if (db.ChangeTracker.HasChanges()) await SaveAsync(cancellationToken);
        return await AccountQuery()
            .Where(account =>
                account.AcademicYear == academicYear
                && account.Semester == semester
                && (!departmentId.HasValue || account.StudentEnrollment!.DepartmentId == departmentId)
                && (!year.HasValue || account.StudentEnrollment!.YearLevel == year.Value)
                && account.StudentEnrollment!.Status == "Active"
                && account.Student!.Status != "Inactive")
            .OrderBy(account => account.Student!.FullName)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<StudentPaymentDto>> MapAsync(
        IReadOnlyCollection<FinancialAccount> accounts,
        CancellationToken cancellationToken)
    {
        if (accounts.Count == 0) return [];
        var periodValues = await db.SystemSettings.AsNoTracking()
            .Where(setting =>
                setting.Section == "academic-year" && setting.Key == "currentYear"
                || setting.Section == "semester" && setting.Key == "currentTerm")
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, cancellationToken);
        var currentYear = periodValues.GetValueOrDefault("academic-year:currentYear", "");
        var currentSemester = periodValues.GetValueOrDefault("semester:currentTerm", "");
        var years = accounts.Select(account => account.AcademicYear).Distinct().ToList();
        var semesters = accounts.Select(account => account.Semester).Distinct().ToList();
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var timetableRows = await db.TimetableEnrollments.AsNoTracking()
            .Include(enrollment => enrollment.Course)
            .Include(enrollment => enrollment.ScheduleEntry)
            .Where(enrollment =>
                years.Contains(enrollment.AcademicYear)
                && semesters.Contains(enrollment.Semester)
                && enrollment.Status == "Active"
                && enrollment.Course != null
                && enrollment.ScheduleEntry != null
                && enrollment.ScheduleEntry.Status != "Cancelled")
            .ToListAsync(cancellationToken);

        return accounts.Select(account =>
        {
            var enrollment = account.StudentEnrollment!;
            var financeCode = codeFormat.PeriodLinkedWithConfiguredPrefix(
                enrollment.EnrollmentCode,
                enrollment.YearLevel,
                enrollment.Semester,
                "financeCodePrefix",
                "FIN");
            var paidPaymentCode = codeFormat.PeriodLinkedWithConfiguredPrefix(
                enrollment.EnrollmentCode,
                enrollment.YearLevel,
                enrollment.Semester,
                "paymentCodePrefix",
                "PAY");
            var timetableReady = timetableRows.Any(timetable =>
                timetable.AcademicYear == account.AcademicYear
                && timetable.Semester == account.Semester
                && timetable.YearLevel == enrollment.YearLevel
                && timetable.Course!.DepartmentId == enrollment.DepartmentId
                && timetable.ScheduleEntry!.Shift == enrollment.Shift);
            var totalDue = TotalDue(account);
            var totalPaid = TotalPaid(account);
            var balance = decimal.Max(0, totalDue - totalPaid);
            var payments = account.Payments
                .OrderByDescending(payment => payment.PaidAtUtc)
                .ThenByDescending(payment => payment.CreateAt)
                .Select(payment => new FinancialPaymentDto(payment.Id, paidPaymentCode, payment.Amount, payment.Method, payment.Status, payment.TransactionReference, payment.PaidAtUtc, payment.CreateAt))
                .ToList();
            var latest = account.Payments.Where(payment => payment.Status == "Completed").OrderByDescending(payment => payment.PaidAtUtc).FirstOrDefault();
            return new StudentPaymentDto(
                account.Id,
                financeCode,
                paidPaymentCode,
                account.StudentId,
                account.Student!.StudentCode,
                enrollment.PublicId,
                account.Student.FullName,
                enrollment.Department?.Name ?? "Unassigned",
                enrollment.EnrollmentCode,
                enrollment.YearLevel,
                enrollment.Shift,
                account.AcademicYear,
                account.Semester,
                account.Title,
                account.PaymentPlan,
                account.DeclaredAmount,
                account.DeclaredAtUtc,
                account.ExpiresAtUtc,
                account.DeclaredAtUtc.HasValue,
                IsExpired(account),
                account.QrGeneratedAtUtc,
                account.QrExpiresAtUtc,
                IsQrExpired(account),
                account.TuitionFee,
                account.OtherFee,
                account.AdjustmentAmount,
                account.AdjustmentReason,
                account.LatePenaltyDays,
                account.LatePenaltyAmount,
                totalDue,
                totalPaid,
                balance,
                balance,
                account.Currency,
                account.DueOn,
                account.Status,
                latest is null ? (totalDue <= 0 ? "No payment required" : string.Empty) : string.IsNullOrWhiteSpace(latest.TransactionReference) ? latest.Method : latest.TransactionReference,
                latest?.PaidAtUtc,
                account.ClosedAtUtc,
                account.Status == "Paid" && balance <= 0m && !account.ClosedAtUtc.HasValue,
                account.ReminderSentAtUtc,
                account.ReminderReadAtUtc,
                timetableReady ? "Ready" : "Waiting",
                account.AcademicYear == currentYear && account.Semester == currentSemester ? "Current" : "Retained",
                "Bakong KHQR",
                AvailableQrPayload(account),
                payments,
                account.CreateAt);
        }).ToList();
    }

    private async Task EnsureLedgerAsync(CancellationToken cancellationToken)
    {
        await synchronizer.EnsureAllAsync(cancellationToken);
        if (db.ChangeTracker.HasChanges()) await SaveAsync(cancellationToken);
    }

    private async Task ApplyLatePenaltiesAsync(
        IReadOnlyCollection<FinancialAccount> accounts,
        CancellationToken cancellationToken)
    {
        if (accounts.Count == 0) return;
        var settings = await settingsReader.GetAsync(cancellationToken);
        var changed = false;
        foreach (var account in accounts) changed = ApplyLatePenalty(account, settings) || changed;
        if (changed) await SaveAsync(cancellationToken);
    }

    private bool ApplyLatePenalty(FinancialAccount account, FinanceSettings settings)
    {
        if (!account.DeclaredAtUtc.HasValue || !account.ExpiresAtUtc.HasValue || account.Status is "Paid" or "Cancelled") return false;
        var lateDays = Math.Max(0, DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - DateOnly.FromDateTime(account.ExpiresAtUtc.Value).DayNumber);
        var lateAmount = decimal.Round(lateDays * settings.LatePenaltyPerDay, 2, MidpointRounding.AwayFromZero);
        if (account.LatePenaltyDays == lateDays && account.LatePenaltyAmount == lateAmount) return false;
        var oldDays = account.LatePenaltyDays;
        var oldAmount = account.LatePenaltyAmount;
        account.LatePenaltyDays = lateDays;
        account.LatePenaltyAmount = lateAmount;
        Recalculate(account);
        AddFinanceAudit(account, "Late punishment updated", new
        {
            account.FinancialAccountCode,
            account.StudentEnrollment!.EnrollmentCode,
            oldDays,
            newDays = lateDays,
            dailyPunishment = settings.LatePenaltyPerDay,
            oldAmount,
            newAmount = lateAmount,
            performedBy = "System"
        });
        return true;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    private Task GenerateQrAsync(FinancialAccount account, FinanceSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var generated = bakong.Generate(account, Balance(account), settings);
        account.BakongQrPayload = generated.Payload;
        account.BakongMd5 = generated.Md5;
        account.QrGeneratedAtUtc = generated.GeneratedAtUtc;
        account.QrExpiresAtUtc = generated.ExpiresAtUtc;
        return Task.CompletedTask;
    }

    private static void EnsureOpen(FinancialAccount account)
    {
        if (account.ClosedAtUtc.HasValue)
            throw new ArgumentException("This semester payment is closed and read-only in Finance history.");
        if (account.Status == "Paid")
            throw new ArgumentException("This student payment is Paid and read-only.");
    }

    private static void ClearQr(FinancialAccount account)
    {
        account.BakongQrPayload = string.Empty;
        account.BakongMd5 = string.Empty;
        account.QrGeneratedAtUtc = null;
        account.QrExpiresAtUtc = null;
    }

    private static void ValidatePayment(
        FinancialAccount account,
        decimal amount,
        string? method,
        string? transactionReference,
        DateTime? paidAtUtc,
        FinanceSettings settings,
        decimal replacedAmount = 0)
    {
        if (account.Status == "Cancelled") throw new ArgumentException("A cancelled financial account cannot receive a payment.");
        if (!account.DeclaredAtUtc.HasValue) throw new ArgumentException("Declare the student payment before recording money.");
        if (amount <= 0) throw new ArgumentException("Payment amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(method) || !settings.PaymentMethods.Any(item => item.Equals(method.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Select an available payment method configured in Settings.");
        if ((transactionReference?.Trim().Length ?? 0) > 256) throw new ArgumentException("Transaction reference cannot exceed 256 characters.");
        if (paidAtUtc?.ToUniversalTime() > DateTime.UtcNow.AddMinutes(5)) throw new ArgumentException("Paid time cannot be in the future.");
        var amountRequired = decimal.Max(0, TotalDue(account) - (TotalPaid(account) - replacedAmount));
        if (!settings.AllowPartialPayments && amount < amountRequired) throw new ArgumentException("Partial payments are disabled in Settings.");
        if (!settings.AllowOverpayment && amount > amountRequired) throw new ArgumentException("Payment amount cannot exceed the current balance.");
    }

    private static string CanonicalMethod(string method, FinanceSettings settings) =>
        settings.PaymentMethods.First(item => item.Equals(method.Trim(), StringComparison.OrdinalIgnoreCase));

    private async Task<string> NextPaymentCodeAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        var enrollment = account.StudentEnrollment
            ?? throw new InvalidOperationException("The financial account must have a Student enrollment.");
        var format = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var financeCode = format.PeriodLinkedWithConfiguredPrefix(
            enrollment.EnrollmentCode,
            enrollment.YearLevel,
            enrollment.Semester,
            "financeCodePrefix",
            "FIN");
        var sequence = await db.FinancialPayments.CountAsync(payment => payment.FinancialAccountId == account.Id, cancellationToken)
            + db.FinancialPayments.Local.Count(payment => payment.FinancialAccountId == account.Id)
            + 1L;
        string code;
        do { code = format.Payment(financeCode, sequence++); }
        while (await db.FinancialPayments.AnyAsync(payment => payment.PaymentCode == code, cancellationToken)
            || db.FinancialPayments.Local.Any(payment => payment.PaymentCode == code));
        return code;
    }

    private static decimal TotalDue(FinancialAccount account) =>
        decimal.Max(0, (account.DeclaredAmount ?? account.TuitionFee + account.OtherFee) + account.AdjustmentAmount + account.LatePenaltyAmount);

    private static decimal TotalPaid(FinancialAccount account) =>
        account.Payments.Where(payment => payment.Status == "Completed").Sum(payment => payment.Amount);

    private static decimal Balance(FinancialAccount account) => decimal.Max(0, TotalDue(account) - TotalPaid(account));

    private static void Recalculate(FinancialAccount account)
    {
        if (account.Status == "Cancelled") return;
        if (!account.DeclaredAtUtc.HasValue)
        {
            account.Status = "Pending";
            account.UpdatedAtUtc = DateTime.UtcNow;
            return;
        }
        var due = TotalDue(account);
        var paid = TotalPaid(account);
        account.Status = due <= 0 || paid >= due
            ? "Paid"
            : paid > 0
                ? "Partial"
                : account.Payments.Any(payment => payment.Status == "Refunded") ? "Refunded" : "Pending";
        account.UpdatedAtUtc = DateTime.UtcNow;
    }

    private void AddFinanceAudit(FinancialAccount account, string action, object details) => db.AuditLogs.Add(new AuditLog
    {
        ResourceId = account.Id,
        Type = "Finance",
        Subject = account.Student?.FullName ?? account.FinancialAccountCode,
        Action = action,
        Details = JsonSerializer.Serialize(details)
    });

    private static bool IsExpired(FinancialAccount account) =>
        account.ExpiresAtUtc.HasValue && account.ExpiresAtUtc.Value <= DateTime.UtcNow && account.Status is not ("Paid" or "Cancelled");

    private static bool SameMinute(DateTime value, DateTime? other) =>
        other.HasValue && Math.Abs((value - other.Value).TotalSeconds) < 60;

    private static bool IsQrExpired(FinancialAccount account) =>
        !string.IsNullOrWhiteSpace(account.BakongQrPayload)
        && account.QrExpiresAtUtc.HasValue
        && account.QrExpiresAtUtc.Value <= DateTime.UtcNow
        && account.Status != "Paid";

    private static string AvailableQrPayload(FinancialAccount account)
    {
        if (!account.DeclaredAtUtc.HasValue || account.Status is "Paid" or "Cancelled" || IsExpired(account) || Balance(account) <= 0) return string.Empty;
        return !string.IsNullOrWhiteSpace(account.BakongQrPayload) && !IsQrExpired(account) ? account.BakongQrPayload : string.Empty;
    }

    private static void ValidateDynamicQrAccount(FinancialAccount account)
    {
        if (!account.DeclaredAtUtc.HasValue) throw new ArgumentException("This payment has not been declared by Finance.");
        if (IsExpired(account)) throw new ArgumentException("This payment declaration has expired. Ask Finance for help.");
        if (account.Status is "Paid" or "Cancelled" || Balance(account) <= 0)
            throw new ArgumentException("This financial account does not need a payment QR.");
    }

    private static bool Matches(string search, params object[] values)
    {
        var term = search.Trim();
        return values.SelectMany(value => value is IEnumerable<string> many ? many : [value?.ToString() ?? string.Empty])
            .Any(value => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static StudentPaymentDto AssertSingle(IReadOnlyList<StudentPaymentDto> accounts) => accounts.Single();
}
