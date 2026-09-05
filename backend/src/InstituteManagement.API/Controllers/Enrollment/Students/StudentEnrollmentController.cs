using InstituteManagement.API.Contracts.Enrollment.Students;
using InstituteManagement.Application.Features.Enrollment.Students.GetStudentEnrollments;
using InstituteManagement.Application.Features.Enrollment.Students.RemoveStudentEnrollment;
using InstituteManagement.Application.Features.Enrollment.Students.UpdateStudentEnrollment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Enrollment.Students;

[ApiController]
[Route(ApiRoutes.Enrollment.Students)]
public sealed class StudentEnrollmentController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStudentEnrollmentsQuery(search, departmentId, year), cancellationToken));

    [HttpPut("{studentId:guid}")]
    public async Task<IActionResult> Update(Guid studentId, StudentEnrollmentValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateStudentEnrollmentCommand(studentId, values), cancellationToken));

    [HttpDelete("{studentId:guid}")]
    public async Task<IActionResult> Remove(Guid studentId, CancellationToken cancellationToken) =>
        await sender.Send(new RemoveStudentEnrollmentCommand(studentId), cancellationToken) ? NoContent() : NotFound();
}
