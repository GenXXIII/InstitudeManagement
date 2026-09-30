using InstituteManagement.API.Contracts.Finance;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Finance.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Finance;

[ApiController]
[Route(ApiRoutes.Finance)]
public sealed class FinanceController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        string? search,
        string? academicYear,
        string? semester,
        string? status,
        Guid? departmentId,
        int? year,
        int page = 1,
        int pageSize = 40,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(
            new GetFinanceAccountsQuery(search, academicYear, semester, status, departmentId, year, new PageRequest(page, pageSize)),
            cancellationToken));

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudent(Guid studentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStudentFinanceAccountsQuery(studentId), cancellationToken));

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetFinanceOptionsQuery(), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/declaration")]
    public async Task<IActionResult> Declare(
        Guid financialAccountId,
        FinanceDeclarationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new DeclareFinanceAccountCommand(financialAccountId, request.ToDto()), cancellationToken));

    [HttpPut("declarations")]
    public async Task<IActionResult> DeclareAll(Guid? departmentId, int? year, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new DeclareAllFinanceAccountsCommand(departmentId, year), cancellationToken));

    [HttpGet("close-readiness")]
    public async Task<IActionResult> GetCloseReadiness(Guid? departmentId, int? year, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetFinanceClosureReadinessQuery(departmentId, year), cancellationToken));

    [HttpPut("close-payments")]
    public async Task<IActionResult> CloseAllPayments(Guid? departmentId, int? year, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CloseAllFinancePaymentsCommand(departmentId, year), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/expiry-extension")]
    public async Task<IActionResult> ExtendExpiry(
        Guid financialAccountId,
        FinanceExpiryExtensionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ExtendFinanceExpiryCommand(financialAccountId, request.ToDto()), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/qr")]
    public async Task<IActionResult> RegenerateQr(Guid financialAccountId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RegenerateFinanceQrCommand(financialAccountId), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/mock-qr")]
    public async Task<IActionResult> GenerateMockPaymentQr(Guid financialAccountId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GenerateMockFinanceQrCommand(financialAccountId), cancellationToken));

    [HttpPut("students/{studentId:guid}/payments/{paymentId:guid}/qr")]
    public async Task<IActionResult> GenerateStudentQr(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GenerateStudentPaymentQrCommand(studentId, paymentId), cancellationToken));

    [HttpPost("students/{studentId:guid}/payments/{paymentId:guid}/verify")]
    public async Task<IActionResult> VerifyStudentPayment(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new VerifyStudentPaymentCommand(studentId, paymentId), cancellationToken));

    [HttpPost("students/{studentId:guid}/payments/{paymentId:guid}/mock-scan")]
    public async Task<IActionResult> ScanMockPaymentQr(
        Guid studentId,
        Guid paymentId,
        MockPaymentScanRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ScanMockPaymentQrCommand(studentId, paymentId, request.ToDto()), cancellationToken));

    [HttpPut("students/{studentId:guid}/payments/{paymentId:guid}/reminder/read")]
    public async Task<IActionResult> MarkReminderRead(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new MarkFinanceReminderReadCommand(studentId, paymentId), cancellationToken));

    [HttpPost("accounts/{financialAccountId:guid}/payments")]
    public async Task<IActionResult> RecordPayment(
        Guid financialAccountId,
        RecordFinancePaymentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RecordFinancePaymentCommand(financialAccountId, request.ToDto()), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/payments/{paymentId:guid}")]
    public async Task<IActionResult> UpdatePayment(
        Guid financialAccountId,
        Guid paymentId,
        UpdateFinancePaymentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateFinancePaymentCommand(financialAccountId, paymentId, request.ToDto()), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/payments/{paymentId:guid}/status")]
    public async Task<IActionResult> SetPaymentStatus(
        Guid financialAccountId,
        Guid paymentId,
        FinancePaymentStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SetFinancePaymentStatusCommand(financialAccountId, paymentId, request.ToDto()), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/adjustment")]
    public async Task<IActionResult> Adjust(
        Guid financialAccountId,
        FinancialAdjustmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new AdjustFinanceAccountCommand(financialAccountId, request.ToDto()), cancellationToken));

    [HttpPut("accounts/{financialAccountId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid financialAccountId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CancelFinanceAccountCommand(financialAccountId), cancellationToken));

}
