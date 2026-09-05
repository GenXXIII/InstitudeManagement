using InstituteManagement.API.Contracts.Enrollment.Teachers;
using InstituteManagement.Application.Features.Enrollment.Teachers.GetTeacherAssignments;
using InstituteManagement.Application.Features.Enrollment.Teachers.RemoveTeacherAssignment;
using InstituteManagement.Application.Features.Enrollment.Teachers.UpdateTeacherAssignment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Enrollment.Teachers;

[ApiController]
[Route(ApiRoutes.Enrollment.Teachers)]
public sealed class TeacherAssignmentController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTeacherAssignmentsQuery(search, departmentId, year), cancellationToken));

    [HttpPut("{teacherId:guid}")]
    public async Task<IActionResult> Update(Guid teacherId, TeacherAssignmentValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateTeacherAssignmentCommand(teacherId, values), cancellationToken));

    [HttpDelete("{teacherId:guid}")]
    public async Task<IActionResult> Remove(Guid teacherId, CancellationToken cancellationToken) =>
        await sender.Send(new RemoveTeacherAssignmentCommand(teacherId), cancellationToken) ? NoContent() : NotFound();
}
