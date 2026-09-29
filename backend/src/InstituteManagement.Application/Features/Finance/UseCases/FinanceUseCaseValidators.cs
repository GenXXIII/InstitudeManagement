using InstituteManagement.Application.Common.Validation;

namespace InstituteManagement.Application.Features.Finance.UseCases;

public sealed class GetStudentFinanceAccountsQueryValidator : IRequestValidator<GetStudentFinanceAccountsQuery>
{
    public IEnumerable<ValidationError> Validate(GetStudentFinanceAccountsQuery request) =>
        FinanceValidation.RequiredId(nameof(request.StudentId), request.StudentId);
}

public sealed class DeclareFinanceAccountCommandValidator : IRequestValidator<DeclareFinanceAccountCommand>
{
    public IEnumerable<ValidationError> Validate(DeclareFinanceAccountCommand request)
    {
        foreach (var error in FinanceValidation.RequiredId(nameof(request.FinancialAccountId), request.FinancialAccountId))
            yield return error;

        var title = request.Declaration.Title?.Trim() ?? string.Empty;
        if (title.Length is < 3 or > 160)
            yield return new(nameof(request.Declaration.Title), "Title must contain 3 to 160 characters.");
        if (!string.Equals(request.Declaration.PaymentPlan?.Trim(), "Semester", StringComparison.Ordinal))
            yield return new(nameof(request.Declaration.PaymentPlan), "PaymentPlan must be Semester.");
        if (request.Declaration.Amount <= 0)
            yield return new(nameof(request.Declaration.Amount), "Amount must be greater than zero.");
    }
}

public sealed class ExtendFinanceExpiryCommandValidator : IRequestValidator<ExtendFinanceExpiryCommand>
{
    public IEnumerable<ValidationError> Validate(ExtendFinanceExpiryCommand request)
    {
        foreach (var error in FinanceValidation.RequiredId(nameof(request.FinancialAccountId), request.FinancialAccountId))
            yield return error;
        if (request.Extension.Days is < 1 or > 365)
            yield return new(nameof(request.Extension.Days), "Days must be between 1 and 365.");
        var reason = request.Extension.Reason?.Trim() ?? string.Empty;
        if (reason.Length is < 3 or > 500)
            yield return new(nameof(request.Extension.Reason), "Reason must contain 3 to 500 characters.");
    }
}

public sealed class RegenerateFinanceQrCommandValidator : IRequestValidator<RegenerateFinanceQrCommand>
{
    public IEnumerable<ValidationError> Validate(RegenerateFinanceQrCommand request) =>
        FinanceValidation.RequiredId(nameof(request.FinancialAccountId), request.FinancialAccountId);
}

public sealed class GenerateMockFinanceQrCommandValidator : IRequestValidator<GenerateMockFinanceQrCommand>
{
    public IEnumerable<ValidationError> Validate(GenerateMockFinanceQrCommand request) =>
        FinanceValidation.RequiredId(nameof(request.FinancialAccountId), request.FinancialAccountId);
}

public sealed class CancelFinanceAccountCommandValidator : IRequestValidator<CancelFinanceAccountCommand>
{
    public IEnumerable<ValidationError> Validate(CancelFinanceAccountCommand request) =>
        FinanceValidation.RequiredId(nameof(request.FinancialAccountId), request.FinancialAccountId);
}

public sealed class GenerateStudentPaymentQrCommandValidator : IRequestValidator<GenerateStudentPaymentQrCommand>
{
    public IEnumerable<ValidationError> Validate(GenerateStudentPaymentQrCommand request) =>
        FinanceValidation.StudentPaymentIds(request.StudentId, request.PaymentId);
}

public sealed class VerifyStudentPaymentCommandValidator : IRequestValidator<VerifyStudentPaymentCommand>
{
    public IEnumerable<ValidationError> Validate(VerifyStudentPaymentCommand request) =>
        FinanceValidation.StudentPaymentIds(request.StudentId, request.PaymentId);
}

public sealed class ScanMockPaymentQrCommandValidator : IRequestValidator<ScanMockPaymentQrCommand>
{
    public IEnumerable<ValidationError> Validate(ScanMockPaymentQrCommand request)
    {
        foreach (var error in FinanceValidation.StudentPaymentIds(request.StudentId, request.PaymentId))
            yield return error;
        if (string.IsNullOrWhiteSpace(request.Scan.QrPayload))
            yield return new(nameof(request.Scan.QrPayload), "QrPayload is required.");
    }
}

public sealed class MarkFinanceReminderReadCommandValidator : IRequestValidator<MarkFinanceReminderReadCommand>
{
    public IEnumerable<ValidationError> Validate(MarkFinanceReminderReadCommand request) =>
        FinanceValidation.StudentPaymentIds(request.StudentId, request.PaymentId);
}

public sealed class RecordFinancePaymentCommandValidator : IRequestValidator<RecordFinancePaymentCommand>
{
    public IEnumerable<ValidationError> Validate(RecordFinancePaymentCommand request)
    {
        foreach (var error in FinanceValidation.RequiredId(nameof(request.FinancialAccountId), request.FinancialAccountId))
            yield return error;
        foreach (var error in FinanceValidation.Payment(request.Payment.Amount, request.Payment.Method, request.Payment.TransactionReference, request.Payment.PaidAtUtc))
            yield return error;
    }
}

public sealed class UpdateFinancePaymentCommandValidator : IRequestValidator<UpdateFinancePaymentCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateFinancePaymentCommand request)
    {
        foreach (var error in FinanceValidation.RequiredId(nameof(request.FinancialAccountId), request.FinancialAccountId))
            yield return error;
        foreach (var error in FinanceValidation.RequiredId(nameof(request.PaymentId), request.PaymentId))
            yield return error;
        foreach (var error in FinanceValidation.Payment(request.Payment.Amount, request.Payment.Method, request.Payment.TransactionReference, request.Payment.PaidAtUtc))
            yield return error;
    }
}

public sealed class SetFinancePaymentStatusCommandValidator : IRequestValidator<SetFinancePaymentStatusCommand>
{
    public IEnumerable<ValidationError> Validate(SetFinancePaymentStatusCommand request)
    {
        foreach (var error in FinanceValidation.RequiredId(nameof(request.FinancialAccountId), request.FinancialAccountId))
            yield return error;
        foreach (var error in FinanceValidation.RequiredId(nameof(request.PaymentId), request.PaymentId))
            yield return error;
        if (request.Status.Status?.Trim() is not ("Cancelled" or "Refunded"))
            yield return new(nameof(request.Status.Status), "Status must be Cancelled or Refunded.");
    }
}

public sealed class AdjustFinanceAccountCommandValidator : IRequestValidator<AdjustFinanceAccountCommand>
{
    public IEnumerable<ValidationError> Validate(AdjustFinanceAccountCommand request)
    {
        foreach (var error in FinanceValidation.RequiredId(nameof(request.FinancialAccountId), request.FinancialAccountId))
            yield return error;
        if (request.Adjustment.Amount != 0 && string.IsNullOrWhiteSpace(request.Adjustment.Reason))
            yield return new(nameof(request.Adjustment.Reason), "Reason is required when Amount is not zero.");
    }
}

internal static class FinanceValidation
{
    public static IEnumerable<ValidationError> RequiredId(string propertyName, Guid value)
    {
        if (value == Guid.Empty)
            yield return new(propertyName, $"{propertyName} is required.");
    }

    public static IEnumerable<ValidationError> StudentPaymentIds(Guid studentId, Guid paymentId)
    {
        foreach (var error in RequiredId("StudentId", studentId)) yield return error;
        foreach (var error in RequiredId("PaymentId", paymentId)) yield return error;
    }

    public static IEnumerable<ValidationError> Payment(decimal amount, string? method, string? reference, DateTime? paidAtUtc)
    {
        if (amount <= 0) yield return new("Amount", "Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(method)) yield return new("Method", "Method is required.");
        if ((reference?.Trim().Length ?? 0) > 256)
            yield return new("TransactionReference", "TransactionReference cannot exceed 256 characters.");
        if (paidAtUtc?.ToUniversalTime() > DateTime.UtcNow.AddMinutes(5))
            yield return new("PaidAtUtc", "PaidAtUtc cannot be in the future.");
    }
}
