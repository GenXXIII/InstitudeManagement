using InstituteManagement.API.Contracts.Enrollment.Classrooms;
using InstituteManagement.Application.Features.Enrollment.Classrooms.GetClassroomAssignments;
using InstituteManagement.Application.Features.Enrollment.Classrooms.RemoveClassroomAssignment;
using InstituteManagement.Application.Features.Enrollment.Classrooms.UpdateClassroomAssignment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Enrollment.Classrooms;

[ApiController]
[Route(ApiRoutes.Enrollment.Classrooms)]
public sealed class ClassroomAssignmentController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetClassroomAssignmentsQuery(search, departmentId, year), cancellationToken));

    [HttpPut("{classroomId:guid}")]
    public async Task<IActionResult> Update(Guid classroomId, ClassroomAssignmentValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateClassroomAssignmentCommand(classroomId, values), cancellationToken));

    [HttpDelete("{classroomId:guid}")]
    public async Task<IActionResult> Remove(Guid classroomId, CancellationToken cancellationToken) =>
        await sender.Send(new RemoveClassroomAssignmentCommand(classroomId), cancellationToken) ? NoContent() : NotFound();
}
