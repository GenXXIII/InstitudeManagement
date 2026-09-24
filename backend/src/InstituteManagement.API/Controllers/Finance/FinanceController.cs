using InstituteManagement.API.Contracts.Finance;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Finance;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Finance;

[ApiController]
[Route(ApiRoutes.Finance)]
public sealed class FinanceController(IFinanceService finance) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        string? search,
        string? academicYear,
        string? semester,
        string? status,
        CancellationToken cancellationToken) =>
        Ok(await finance.GetAsync(search, academicYear, semester, status, cancellationToken));

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudent(Guid studentId, CancellationToken cancellationToken) =>
        Ok(await finance.GetStudentAsync(studentId, cancellationToken));

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken) =>
        Ok(await finance.GetOptionsAsync(cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/declaration")]
    public async Task<IActionResult> Declare(
        Guid financialAccountId,
        FinanceDeclarationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await finance.DeclareAsync(financialAccountId, request.ToDto(), cancellationToken));

    [HttpPut("declarations")]
    public async Task<IActionResult> DeclareAll(CancellationToken cancellationToken) =>
        Ok(await finance.DeclareAllAsync(cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/expiry-extension")]
    public async Task<IActionResult> ExtendExpiry(
        Guid financialAccountId,
        FinanceExpiryExtensionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await finance.ExtendExpiryAsync(financialAccountId, request.ToDto(), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/qr")]
    public async Task<IActionResult> RegenerateQr(Guid financialAccountId, CancellationToken cancellationToken) =>
        Ok(await finance.RegenerateQrAsync(financialAccountId, cancellationToken));

    [HttpPut("students/{studentId:guid}/payments/{paymentId:guid}/qr")]
    public async Task<IActionResult> GenerateStudentQr(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        Ok(await finance.GenerateStudentQrAsync(studentId, paymentId, cancellationToken));

    [HttpPost("students/{studentId:guid}/payments/{paymentId:guid}/verify")]
    public async Task<IActionResult> VerifyStudentPayment(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        Ok(await finance.VerifyStudentPaymentAsync(studentId, paymentId, cancellationToken));

    [HttpPut("students/{studentId:guid}/payments/{paymentId:guid}/reminder/read")]
    public async Task<IActionResult> MarkReminderRead(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        Ok(await finance.MarkReminderReadAsync(studentId, paymentId, cancellationToken));

    [HttpPost("accounts/{financialAccountId:guid}/payments")]
    public async Task<IActionResult> RecordPayment(
        Guid financialAccountId,
        RecordFinancePaymentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await finance.RecordPaymentAsync(financialAccountId, request.ToDto(), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/payments/{paymentId:guid}")]
    public async Task<IActionResult> UpdatePayment(
        Guid financialAccountId,
        Guid paymentId,
        UpdateFinancePaymentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await finance.UpdatePaymentAsync(financialAccountId, paymentId, request.ToDto(), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/payments/{paymentId:guid}/status")]
    public async Task<IActionResult> SetPaymentStatus(
        Guid financialAccountId,
        Guid paymentId,
        FinancePaymentStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await finance.SetPaymentStatusAsync(financialAccountId, paymentId, request.ToDto(), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/adjustment")]
    public async Task<IActionResult> Adjust(
        Guid financialAccountId,
        FinancialAdjustmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await finance.AdjustAsync(financialAccountId, request.ToDto(), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid financialAccountId, CancellationToken cancellationToken) =>
        Ok(await finance.CancelAsync(financialAccountId, cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/close-payment")]
    public async Task<IActionResult> ClosePayment(Guid financialAccountId, CancellationToken cancellationToken) =>
        Ok(await finance.ClosePaymentAsync(financialAccountId, cancellationToken));
}
