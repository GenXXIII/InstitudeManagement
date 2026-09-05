using InstituteManagement.API.Contracts.Management.Classrooms;
using InstituteManagement.Application.Features.Management.Classrooms.CreateClassroom;
using InstituteManagement.Application.Features.Management.Classrooms.DeleteClassroom;
using InstituteManagement.Application.Features.Management.Classrooms.GetClassrooms;
using InstituteManagement.Application.Features.Management.Classrooms.UpdateClassroom;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Management.Classrooms;

[ApiController]
[Route(ApiRoutes.Catalog.Classrooms)]
public sealed class ClassroomsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetClassroomsQuery(search, departmentId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(ClassroomValuesRequest values, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new CreateClassroomCommand(values), cancellationToken);
        return Created($"/{ApiRoutes.Catalog.Classrooms}/{item.Id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ClassroomValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateClassroomCommand(id, values), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new DeleteClassroomCommand(id), cancellationToken) ? NoContent() : NotFound();
}
