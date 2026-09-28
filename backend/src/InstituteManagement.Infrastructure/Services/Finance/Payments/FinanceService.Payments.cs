using InstituteManagement.Application.Features.Finance;
using InstituteManagement.Application.Features.Enrollment.Students.Progression;
using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed partial class FinanceService
{
    public async Task<StudentPaymentDto> GenerateStudentQrAsync(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var account = await FindStudentAccountAsync(studentId, paymentId, cancellationToken);
        EnsureOpen(account);
        ValidateDynamicQrAccount(account);
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (!settings.BakongEnabled) throw new InvalidOperationException("Dynamic KHQR is not enabled in Finance Settings.");
        await GenerateQrAsync(account, settings, cancellationToken);
        AddFinanceAudit(account, "Student dynamic KHQR generated", new
        {
            account.FinancialAccountCode,
            account.StudentEnrollment!.EnrollmentCode,
            amount = Balance(account),
            account.Currency,
            account.QrGeneratedAtUtc,
            account.QrExpiresAtUtc,
            performedBy = "Student"
        });
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    public async Task<StudentPaymentDto> VerifyStudentPaymentAsync(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var account = await FindStudentAccountAsync(studentId, paymentId, cancellationToken);
        if (account.Status == "Paid") return AssertSingle(await MapAsync([account], cancellationToken));
        ValidateDynamicQrAccount(account);
        if (string.IsNullOrWhiteSpace(account.BakongQrPayload) || string.IsNullOrWhiteSpace(account.BakongMd5))
            throw new ArgumentException("Generate this payment QR before checking its payment status.");
        if (IsQrExpired(account)) throw new ArgumentException("This payment QR has expired. Generate a new payment QR.");

        var settings = await settingsReader.GetAsync(cancellationToken);
        if (!settings.BakongEnabled) throw new InvalidOperationException("Dynamic KHQR is not enabled in Finance Settings.");
        var verified = await bakong.VerifyAsync(account, Balance(account), settings, cancellationToken);
        if (await db.FinancialPayments.AnyAsync(payment => payment.TransactionReference == verified.TransactionHash && payment.Status == "Completed", cancellationToken))
            throw new InvalidOperationException("This Bakong transaction has already been used for another payment.");
        return await RecordPaymentInternalAsync(
            account,
            new RecordFinancePaymentDto(Balance(account), "Bakong", verified.TransactionHash, verified.PaidAtUtc),
            $"Bakong API · {verified.FromAccountId}",
            cancellationToken);
    }

    public async Task<StudentPaymentDto> RegenerateQrAsync(Guid financialAccountId, CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        EnsureOpen(account);
        if (!account.DeclaredAtUtc.HasValue) throw new ArgumentException("Declare the student payment before generating its QR.");
        if (IsExpired(account)) throw new ArgumentException("The payment declaration has expired. Extend it before regenerating its QR.");
        if (account.Status is "Paid" or "Cancelled" || Balance(account) <= 0) throw new ArgumentException("This financial account does not need a payment QR.");
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (!settings.BakongEnabled) throw new InvalidOperationException("Enable Bakong KHQR in Settings before regenerating an official payment QR.");
        await GenerateQrAsync(account, settings, cancellationToken);
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
        EnsureOpen(account);
        return await RecordPaymentInternalAsync(account, request, "Administrator", cancellationToken);
    }

    public async Task<StudentPaymentDto> UpdatePaymentAsync(
        Guid financialAccountId,
        Guid paymentId,
        UpdateFinancePaymentDto request,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        EnsureOpen(account);
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
        ClearQr(account);
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
        EnsureOpen(account);
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
        ClearQr(account);
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
        EnsureOpen(account);
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
        ClearQr(account);
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
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    public async Task<StudentPaymentDto> CancelAsync(Guid financialAccountId, CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        EnsureOpen(account);
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

    public async Task<MockPaymentQrDto> GenerateMockPaymentQrAsync(
        Guid financialAccountId,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        EnsureOpen(account);
        ValidateDynamicQrAccount(account);
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (!settings.MockPaymentEnabled)
            throw new InvalidOperationException("Enable Mock Scan QR Pay in Finance Settings before generating a test QR.");

        var generated = mockPayments.Generate(account, account.StudentEnrollment!.PublicId, Balance(account));
        AddFinanceAudit(account, "Mock payment QR generated", new
        {
            account.FinancialAccountCode,
            account.StudentEnrollment.EnrollmentCode,
            generated.PublicId,
            amount = Balance(account),
            account.Currency,
            generated.GeneratedAtUtc,
            generated.ExpiresAtUtc,
            performedBy = "Administrator"
        });
        await SaveAsync(cancellationToken);
        return new(generated.Payload, generated.PublicId, generated.GeneratedAtUtc, generated.ExpiresAtUtc);
    }

    public async Task<StudentPaymentDto> ScanMockPaymentQrAsync(
        Guid studentId,
        Guid paymentId,
        MockPaymentScanDto request,
        CancellationToken cancellationToken)
    {
        var account = await FindStudentAccountAsync(studentId, paymentId, cancellationToken);
        EnsureOpen(account);
        ValidateDynamicQrAccount(account);
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (!settings.MockPaymentEnabled)
            throw new InvalidOperationException("Mock Scan QR Pay is disabled in Finance Settings.");

        var claims = mockPayments.Validate(request.QrPayload);
        var publicId = account.StudentEnrollment!.PublicId;
        if (claims.FinancialAccountId != account.Id
            || claims.StudentId != account.StudentId
            || !claims.PublicId.Equals(publicId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("This mock payment QR belongs to a different student payment.");
        if (claims.Amount != decimal.Round(Balance(account), 2)
            || !claims.Currency.Equals(account.Currency, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("This mock payment QR no longer matches the current payment balance.");

        var reference = $"MOCK-{claims.TokenId}";
        if (await db.FinancialPayments.AnyAsync(payment => payment.TransactionReference == reference, cancellationToken))
            throw new ArgumentException("This mock payment QR has already been used.");
        return await RecordPaymentInternalAsync(
            account,
            new RecordFinancePaymentDto(Balance(account), "Mock QR", reference, DateTime.UtcNow),
            $"Student Mock QR scan · {publicId}",
            cancellationToken);
    }

    public async Task<StudentPaymentDto> ClosePaymentAsync(Guid financialAccountId, CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        if (account.ClosedAtUtc.HasValue) return AssertSingle(await MapAsync([account], cancellationToken));
        if (account.Status != "Paid" || Balance(account) > 0m)
            throw new ArgumentException("Payment can only be closed after the full balance is paid.");

        account.ClosedAtUtc = DateTime.UtcNow;
        account.UpdatedAtUtc = account.ClosedAtUtc.Value;
        ClearQr(account);
        var progressionResult = await progression.ReleaseAsync(
            new StudentEnrollmentProgressionRequest(
                account.StudentId,
                account.StudentEnrollmentId,
                account.AcademicYear,
                account.Semester,
                account.FinancialAccountCode,
                account.Status),
            cancellationToken);
        AddFinanceAudit(account, "Payment closed", new
        {
            account.FinancialAccountCode,
            account.StudentEnrollment!.EnrollmentCode,
            account.AcademicYear,
            account.Semester,
            account.Status,
            account.ClosedAtUtc,
            progression = progressionResult,
            historyState = "Finalized",
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
        EnsureOpen(account);
        var settings = await settingsReader.GetAsync(cancellationToken);
        ValidatePayment(account, request.Amount, request.Method, request.TransactionReference, request.PaidAtUtc, settings);
        var oldStatus = account.Status;
        var oldBalance = Balance(account);
        var payment = new FinancialPayment
        {
            PaymentCode = await NextPaymentCodeAsync(account, cancellationToken),
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
        ClearQr(account);
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
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }
}
