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

    [HttpPost("students/{studentId:guid}/payments/{paymentId:guid}/confirm")]
    public async Task<IActionResult> Confirm(
        Guid studentId,
        Guid paymentId,
        StudentPaymentConfirmationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await finance.ConfirmAsync(studentId, paymentId, request.ToDto(), cancellationToken));

    [HttpPut("students/{studentId:guid}/payments/{paymentId:guid}/reminder/read")]
    public async Task<IActionResult> MarkReminderRead(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        Ok(await finance.MarkReminderReadAsync(studentId, paymentId, cancellationToken));
}
