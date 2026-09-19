using System.Globalization;
using System.Text.Json;
using InstituteManagement.Application.Features.Finance;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed class FinanceService(
    InstituteDbContext db,
    InstituteCache cache,
    FinancialAccountSynchronizer synchronizer,
    FinancialProgression progression,
    FinanceSettingsReader settingsReader,
    BakongPaymentGateway bakong) : IFinanceService
{
    public async Task<IReadOnlyList<StudentPaymentDto>> GetAsync(
        string? search,
        string? academicYear,
        string? semester,
        string? status,
        CancellationToken cancellationToken)
    {
        await EnsureLedgerAsync(cancellationToken);
        var accounts = await AccountQuery()
            .Where(account =>
                (string.IsNullOrWhiteSpace(academicYear) || account.AcademicYear == academicYear)
                && (string.IsNullOrWhiteSpace(semester) || account.Semester == semester))
            .OrderBy(account => account.Status == "Paid")
            .ThenBy(account => account.DueOn)
            .ThenBy(account => account.Student!.FullName)
            .ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(search))
        {
            accounts = accounts.Where(account => Matches(
                search,
                account.FinancialAccountCode,
                account.Student!.StudentCode,
                account.Student.PublicId,
                account.Student.FullName,
                account.StudentEnrollment!.EnrollmentCode,
                account.StudentEnrollment.Department!.Name,
                account.Title,
                account.PaymentPlan,
                account.Payments.Select(payment => payment.PaymentCode).ToArray())).ToList();
        }

        var mapped = await MapAsync(accounts, cancellationToken);
        return string.IsNullOrWhiteSpace(status) || status == "All"
            ? mapped
            : mapped.Where(account => account.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<IReadOnlyList<StudentPaymentDto>> GetStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        if (!await db.Students.AsNoTracking().AnyAsync(student => student.Id == studentId && student.Status != "Inactive", cancellationToken))
            throw new KeyNotFoundException("Active student not found.");
        await EnsureLedgerAsync(cancellationToken);
        var accounts = await AccountQuery()
            .Where(account => account.StudentId == studentId && account.DeclaredAtUtc != null)
            .OrderBy(account => account.Status == "Paid")
            .ThenByDescending(account => account.AcademicYear)
            .ThenByDescending(account => account.Semester)
            .ToListAsync(cancellationToken);
        return await MapAsync(accounts, cancellationToken);
    }

    public async Task<FinanceOptionsDto> GetOptionsAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsReader.GetAsync(cancellationToken);
        return new(settings.PaymentMethods, settings.AllowPartialPayments, settings.AllowOverpayment, settings.MaximumAdjustmentAmount, settings.RequirePaidForAdvancement, settings.BakongEnabled, bakong.IsConfigured(settings), settings.BakongEnvironment);
    }

    public async Task<StudentPaymentDto> DeclareAsync(
        Guid financialAccountId,
        FinanceDeclarationDto request,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (account.Status == "Cancelled") throw new ArgumentException("A cancelled financial account cannot be declared.");
        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length is < 3 or > 160) throw new ArgumentException("Declaration title must contain 3 to 160 characters.");
        var paymentPlan = request.PaymentPlan?.Trim();
        if (paymentPlan is not ("Semester" or "Year")) throw new ArgumentException("Payment plan must be Semester or Year.");
        if (paymentPlan == "Year" && account.Semester != "Semester 1")
            throw new ArgumentException("A Year payment can only be declared from Semester 1 and covers Semester 1 and Semester 2.");
        var amount = decimal.Round(request.Amount, 2);
        if (amount <= 0) throw new ArgumentException("Declaration amount must be greater than zero.");
        var expiresAtUtc = request.ExpiresAtUtc.ToUniversalTime();
        if (expiresAtUtc <= DateTime.UtcNow) throw new ArgumentException("QR expiry must be in the future.");
        if (DateOnly.FromDateTime(expiresAtUtc) < request.DueOn) throw new ArgumentException("QR expiry cannot be earlier than the due date.");
        if (amount + account.AdjustmentAmount < TotalPaid(account))
            throw new ArgumentException("Declaration amount plus its adjustment cannot be lower than money already paid.");

        var wasDeclared = account.DeclaredAtUtc.HasValue;
        var oldTitle = account.Title;
        var oldPlan = account.PaymentPlan;
        var oldAmount = account.DeclaredAmount;
        var oldDueOn = account.DueOn;
        var oldExpiresAtUtc = account.ExpiresAtUtc;
        var oldStatus = account.Status;
        var oldBalance = Balance(account);
        account.Title = title;
        account.PaymentPlan = paymentPlan;
        account.DeclaredAmount = amount;
        account.DeclaredAtUtc ??= DateTime.UtcNow;
        account.DueOn = request.DueOn;
        account.ExpiresAtUtc = expiresAtUtc;
        Recalculate(account);
        await RefreshQrAsync(account, settings, cancellationToken);
        AddFinanceAudit(account, wasDeclared ? "Payment declaration updated" : "Payment declared", new
        {
            account.FinancialAccountCode,
            account.StudentEnrollment!.EnrollmentCode,
            oldTitle,
            newTitle = account.Title,
            oldPlan,
            newPlan = account.PaymentPlan,
            oldAmount,
            newAmount = account.DeclaredAmount,
            oldDueOn,
            newDueOn = account.DueOn,
            oldExpiresAtUtc,
            newExpiresAtUtc = account.ExpiresAtUtc,
            oldFinancialState = oldStatus,
            newFinancialState = account.Status,
            oldBalance,
            newBalance = Balance(account),
            performedBy = "Administrator"
        });
        if (oldStatus != "Paid" && account.Status == "Paid") await progression.ReleaseAsync(account, cancellationToken);
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    public async Task<StudentPaymentDto> ConfirmAsync(
        Guid studentId,
        Guid paymentId,
        StudentPaymentConfirmationDto request,
        CancellationToken cancellationToken)
    {
        var account = await AccountQuery().SingleOrDefaultAsync(item => item.Id == paymentId && item.StudentId == studentId, cancellationToken)
            ?? throw new KeyNotFoundException("Student financial account not found.");
        if (!account.DeclaredAtUtc.HasValue) throw new ArgumentException("This payment has not been declared by Finance.");
        if (IsExpired(account)) throw new ArgumentException("This payment QR has expired. Ask Finance to update the declaration.");
        if (account.Status == "Paid") return AssertSingle(await MapAsync([account], cancellationToken));

        var settings = await settingsReader.GetAsync(cancellationToken);
        if (settings.BakongEnabled)
        {
            if (string.IsNullOrWhiteSpace(account.BakongQrPayload) || request.QrPayload?.Trim() != account.BakongQrPayload)
                throw new ArgumentException("The scanned Bakong KHQR does not match this student's current payment declaration.");
            var verified = await bakong.VerifyAsync(account, Balance(account), settings, cancellationToken);
            if (await db.FinancialPayments.AnyAsync(payment => payment.TransactionReference == verified.TransactionHash && payment.Status == "Completed", cancellationToken))
                throw new InvalidOperationException("This Bakong transaction has already been used for another payment.");
            return await RecordPaymentInternalAsync(
                account,
                new RecordFinancePaymentDto(Balance(account), "Bakong", verified.TransactionHash, verified.PaidAtUtc),
                $"Bakong API · {verified.FromAccountId}",
                cancellationToken);
        }
        if (request.QrPayload?.Trim() != QrPayload(account))
            throw new ArgumentException("The scanned QR code does not match this student's current financial balance.");
        var method = settings.PaymentMethods.FirstOrDefault(item => item.Equals("Other", StringComparison.OrdinalIgnoreCase))
            ?? settings.PaymentMethods[0];
        return await RecordPaymentInternalAsync(
            account,
            new RecordFinancePaymentDto(Balance(account), method, "Student QR scan", DateTime.UtcNow),
            "Student QR scan",
            cancellationToken);
    }

    public async Task<StudentPaymentDto> RegenerateQrAsync(Guid financialAccountId, CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        if (!account.DeclaredAtUtc.HasValue) throw new ArgumentException("Declare the student payment before generating its QR.");
        if (IsExpired(account)) throw new ArgumentException("The payment declaration has expired. Extend it before regenerating its QR.");
        if (account.Status is "Paid" or "Cancelled" || Balance(account) <= 0) throw new ArgumentException("This financial account does not need a payment QR.");
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (!settings.BakongEnabled) throw new InvalidOperationException("Enable Bakong KHQR in Settings before regenerating an official payment QR.");
        await RefreshQrAsync(account, settings, cancellationToken);
        AddFinanceAudit(account, "Bakong KHQR regenerated", new
        {
            account.FinancialAccountCode,
            account.StudentEnrollment!.EnrollmentCode,
            amount = Balance(account),
            account.Currency,
            account.QrGeneratedAtUtc,
            account.QrExpiresAtUtc,
            performedBy = "Administrator"
        });
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    public async Task<StudentPaymentDto> MarkReminderReadAsync(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var account = await AccountQuery().SingleOrDefaultAsync(item => item.Id == paymentId && item.StudentId == studentId, cancellationToken)
            ?? throw new KeyNotFoundException("Student financial account not found.");
        if (account.ReminderSentAtUtc.HasValue && !account.ReminderReadAtUtc.HasValue)
        {
            account.ReminderReadAtUtc = DateTime.UtcNow;
            account.UpdatedAtUtc = DateTime.UtcNow;
            await SaveAsync(cancellationToken);
        }
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    public async Task<StudentPaymentDto> RecordPaymentAsync(
        Guid financialAccountId,
        RecordFinancePaymentDto request,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        return await RecordPaymentInternalAsync(account, request, "Administrator", cancellationToken);
    }

    public async Task<StudentPaymentDto> UpdatePaymentAsync(
        Guid financialAccountId,
        Guid paymentId,
        UpdateFinancePaymentDto request,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        var payment = account.Payments.SingleOrDefault(item => item.Id == paymentId)
            ?? throw new KeyNotFoundException("Finance payment not found.");
        if (payment.Status != "Completed") throw new ArgumentException("Only a completed payment can be corrected.");
        var settings = await settingsReader.GetAsync(cancellationToken);
        ValidatePayment(account, request.Amount, request.Method, request.TransactionReference, request.PaidAtUtc, settings, payment.Amount);
        var oldAmount = payment.Amount;
        var oldMethod = payment.Method;
        var oldReference = payment.TransactionReference;
        var oldPaidAt = payment.PaidAtUtc;
        var oldStatus = account.Status;
        var oldBalance = Balance(account);

        payment.Amount = decimal.Round(request.Amount, 2);
        payment.Method = CanonicalMethod(request.Method!, settings);
        payment.TransactionReference = request.TransactionReference?.Trim() ?? string.Empty;
        payment.PaidAtUtc = request.PaidAtUtc?.ToUniversalTime() ?? payment.PaidAtUtc;
        payment.UpdatedAtUtc = DateTime.UtcNow;
        Recalculate(account);
        await RefreshQrAsync(account, settings, cancellationToken);
        AddFinanceAudit(account, "Payment updated", new
        {
            account.FinancialAccountCode,
            payment.PaymentCode,
            account.StudentEnrollment!.EnrollmentCode,
            oldAmount,
            newAmount = payment.Amount,
            oldMethod,
            newMethod = payment.Method,
            oldReference,
            newReference = payment.TransactionReference,
            oldPaidAt,
            newPaidAt = payment.PaidAtUtc,
            oldFinancialState = oldStatus,
            newFinancialState = account.Status,
            oldBalance,
            newBalance = Balance(account),
            performedBy = "Administrator"
        });
        if (oldStatus != "Paid" && account.Status == "Paid") await progression.ReleaseAsync(account, cancellationToken);
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    public async Task<StudentPaymentDto> SetPaymentStatusAsync(
        Guid financialAccountId,
        Guid paymentId,
        FinancePaymentStatusDto request,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        var payment = account.Payments.SingleOrDefault(item => item.Id == paymentId)
            ?? throw new KeyNotFoundException("Finance payment not found.");
        var nextStatus = request.Status?.Trim();
        if (nextStatus is not ("Cancelled" or "Refunded")) throw new ArgumentException("Payment status must be Cancelled or Refunded.");
        if (payment.Status == nextStatus) return AssertSingle(await MapAsync([account], cancellationToken));
        if (payment.Status != "Completed") throw new ArgumentException("Only a completed payment can be cancelled or refunded.");
        var oldPaymentState = payment.Status;
        var oldFinancialState = account.Status;
        var oldBalance = Balance(account);
        payment.Status = nextStatus;
        payment.UpdatedAtUtc = DateTime.UtcNow;
        Recalculate(account);
        await RefreshQrAsync(account, await settingsReader.GetAsync(cancellationToken), cancellationToken);
        AddFinanceAudit(account, nextStatus == "Refunded" ? "Payment refunded" : "Payment cancelled", new
        {
            account.FinancialAccountCode,
            payment.PaymentCode,
            account.StudentEnrollment!.EnrollmentCode,
            payment.Amount,
            payment.Method,
            oldPaymentState,
            newPaymentState = payment.Status,
            oldFinancialState,
            newFinancialState = account.Status,
            oldBalance,
            newBalance = Balance(account),
            performedBy = "Administrator"
        });
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    public async Task<StudentPaymentDto> AdjustAsync(
        Guid financialAccountId,
        FinancialAdjustmentDto request,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        if (account.Status == "Cancelled") throw new ArgumentException("A cancelled financial account cannot be adjusted.");
        var settings = await settingsReader.GetAsync(cancellationToken);
        var amount = decimal.Round(request.Amount, 2);
        if (Math.Abs(amount) > settings.MaximumAdjustmentAmount) throw new ArgumentException($"Adjustment cannot exceed {settings.MaximumAdjustmentAmount:0.00}.");
        if ((account.DeclaredAmount ?? account.TuitionFee + account.OtherFee) + amount < 0) throw new ArgumentException("A discount cannot reduce the total due below zero.");
        if (amount != 0 && string.IsNullOrWhiteSpace(request.Reason)) throw new ArgumentException("An adjustment reason is required.");
        var oldAmount = account.AdjustmentAmount;
        var oldReason = account.AdjustmentReason;
        var oldStatus = account.Status;
        var oldBalance = Balance(account);
        account.AdjustmentAmount = amount;
        account.AdjustmentReason = request.Reason?.Trim() ?? string.Empty;
        Recalculate(account);
        await RefreshQrAsync(account, settings, cancellationToken);
        AddFinanceAudit(account, "Financial adjustment updated", new
        {
            account.FinancialAccountCode,
            account.StudentEnrollment!.EnrollmentCode,
            oldAmount,
            newAmount = account.AdjustmentAmount,
            oldReason,
            newReason = account.AdjustmentReason,
            oldFinancialState = oldStatus,
            newFinancialState = account.Status,
            oldBalance,
            newBalance = Balance(account),
            performedBy = "Administrator"
        });
        if (oldStatus != "Paid" && account.Status == "Paid") await progression.ReleaseAsync(account, cancellationToken);
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    public async Task<StudentPaymentDto> CancelAsync(Guid financialAccountId, CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        if (account.Status == "Cancelled") return AssertSingle(await MapAsync([account], cancellationToken));
        if (account.Payments.Any(payment => payment.Status == "Completed"))
            throw new ArgumentException("Cancel or refund completed payments before cancelling the financial account.");
        var oldStatus = account.Status;
        account.Status = "Cancelled";
        account.UpdatedAtUtc = DateTime.UtcNow;
        AddFinanceAudit(account, "Financial account cancelled", new
        {
            account.FinancialAccountCode,
            account.StudentEnrollment!.EnrollmentCode,
            oldFinancialState = oldStatus,
            newFinancialState = account.Status,
            balance = Balance(account),
            performedBy = "Administrator"
        });
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    private async Task<StudentPaymentDto> RecordPaymentInternalAsync(
        FinancialAccount account,
        RecordFinancePaymentDto request,
        string performedBy,
        CancellationToken cancellationToken)
    {
        var settings = await settingsReader.GetAsync(cancellationToken);
        ValidatePayment(account, request.Amount, request.Method, request.TransactionReference, request.PaidAtUtc, settings);
        var oldStatus = account.Status;
        var oldBalance = Balance(account);
        var payment = new FinancialPayment
        {
            PaymentCode = await NextPaymentCodeAsync(cancellationToken),
            FinancialAccountId = account.Id,
            Amount = decimal.Round(request.Amount, 2),
            Method = CanonicalMethod(request.Method!, settings),
            Status = "Completed",
            TransactionReference = request.TransactionReference?.Trim() ?? string.Empty,
            PaidAtUtc = request.PaidAtUtc?.ToUniversalTime() ?? DateTime.UtcNow
        };
        account.Payments.Add(payment);
        db.FinancialPayments.Add(payment);
        account.ReminderReadAtUtc = DateTime.UtcNow;
        Recalculate(account);
        await RefreshQrAsync(account, settings, cancellationToken);
        AddFinanceAudit(account, "Payment recorded", new
        {
            account.FinancialAccountCode,
            payment.PaymentCode,
            account.StudentEnrollment!.EnrollmentCode,
            payment.Amount,
            payment.Method,
            payment.TransactionReference,
            payment.PaidAtUtc,
            oldFinancialState = oldStatus,
            newFinancialState = account.Status,
            oldBalance,
            newBalance = Balance(account),
            performedBy
        });
        if (oldStatus != "Paid" && account.Status == "Paid") await progression.ReleaseAsync(account, cancellationToken);
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    private IQueryable<FinancialAccount> AccountQuery() => db.FinancialAccounts
        .Include(account => account.Payments)
        .Include(account => account.Student)
        .Include(account => account.StudentEnrollment)!
            .ThenInclude(enrollment => enrollment!.Department);

    private async Task<FinancialAccount> FindAccountAsync(Guid id, CancellationToken cancellationToken) =>
        await AccountQuery().SingleOrDefaultAsync(account => account.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Financial account not found.");

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
                .Select(payment => new FinancialPaymentDto(payment.Id, payment.PaymentCode, payment.Amount, payment.Method, payment.Status, payment.TransactionReference, payment.PaidAtUtc, payment.CreateAt))
                .ToList();
            var latest = account.Payments.Where(payment => payment.Status == "Completed").OrderByDescending(payment => payment.PaidAtUtc).FirstOrDefault();
            return new StudentPaymentDto(
                account.Id,
                account.FinancialAccountCode,
                account.FinancialAccountCode,
                account.StudentId,
                account.Student!.StudentCode,
                account.Student.PublicId,
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
                totalDue,
                totalPaid,
                balance,
                balance,
                account.Currency,
                account.DueOn,
                account.Status,
                latest is null ? (totalDue <= 0 ? "No payment required" : string.Empty) : string.IsNullOrWhiteSpace(latest.TransactionReference) ? latest.Method : latest.TransactionReference,
                latest?.PaidAtUtc,
                account.ReminderSentAtUtc,
                account.ReminderReadAtUtc,
                timetableReady ? "Ready" : "Waiting",
                account.AcademicYear == currentYear && account.Semester == currentSemester ? "Current" : "Retained",
                string.IsNullOrWhiteSpace(account.BakongQrPayload) && latest?.Method != "Bakong" ? "Simulated" : "Bakong KHQR",
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

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    private Task RefreshQrAsync(FinancialAccount account, FinanceSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!settings.BakongEnabled || !account.DeclaredAtUtc.HasValue || account.Status is "Paid" or "Cancelled" || IsExpired(account) || Balance(account) <= 0)
        {
            account.BakongQrPayload = string.Empty;
            account.BakongMd5 = string.Empty;
            account.QrGeneratedAtUtc = null;
            account.QrExpiresAtUtc = null;
            return Task.CompletedTask;
        }

        var generated = bakong.Generate(account, Balance(account), settings);
        account.BakongQrPayload = generated.Payload;
        account.BakongMd5 = generated.Md5;
        account.QrGeneratedAtUtc = generated.GeneratedAtUtc;
        account.QrExpiresAtUtc = generated.ExpiresAtUtc;
        return Task.CompletedTask;
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
        if (IsExpired(account)) throw new ArgumentException("The payment declaration has expired. Update its expiry before recording money.");
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

    private async Task<string> NextPaymentCodeAsync(CancellationToken cancellationToken)
    {
        var sequence = await db.FinancialPayments.CountAsync(cancellationToken) + db.FinancialPayments.Local.Count + 1;
        string code;
        do { code = $"P-{sequence++:D5}"; }
        while (await db.FinancialPayments.AnyAsync(payment => payment.PaymentCode == code, cancellationToken)
            || db.FinancialPayments.Local.Any(payment => payment.PaymentCode == code));
        return code;
    }

    private static decimal TotalDue(FinancialAccount account) =>
        decimal.Max(0, (account.DeclaredAmount ?? account.TuitionFee + account.OtherFee) + account.AdjustmentAmount);

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

    private static string QrPayload(FinancialAccount account) =>
        $"INK-PAY|{account.Id:N}|{account.StudentId:N}|{Balance(account).ToString("0.00", CultureInfo.InvariantCulture)}|{account.Currency}|{account.PaymentPlan}|{account.ExpiresAtUtc?.Ticks ?? 0}";

    private static bool IsExpired(FinancialAccount account) =>
        account.ExpiresAtUtc.HasValue && account.ExpiresAtUtc.Value <= DateTime.UtcNow && account.Status != "Paid";

    private static bool IsQrExpired(FinancialAccount account) =>
        !string.IsNullOrWhiteSpace(account.BakongQrPayload)
        && account.QrExpiresAtUtc.HasValue
        && account.QrExpiresAtUtc.Value <= DateTime.UtcNow
        && account.Status != "Paid";

    private static string AvailableQrPayload(FinancialAccount account)
    {
        if (!account.DeclaredAtUtc.HasValue || account.Status is "Paid" or "Cancelled" || IsExpired(account) || Balance(account) <= 0) return string.Empty;
        if (!string.IsNullOrWhiteSpace(account.BakongQrPayload)) return IsQrExpired(account) ? string.Empty : account.BakongQrPayload;
        return QrPayload(account);
    }

    private static bool Matches(string search, params object[] values)
    {
        var term = search.Trim();
        return values.SelectMany(value => value is IEnumerable<string> many ? many : [value?.ToString() ?? string.Empty])
            .Any(value => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static StudentPaymentDto AssertSingle(IReadOnlyList<StudentPaymentDto> accounts) => accounts.Single();
}
