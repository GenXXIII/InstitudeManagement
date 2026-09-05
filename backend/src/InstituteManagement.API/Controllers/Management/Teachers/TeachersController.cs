using InstituteManagement.API.Contracts.Management.Teachers;
using InstituteManagement.Application.Features.Management.Teachers.CreateTeacher;
using InstituteManagement.Application.Features.Management.Teachers.DeleteTeacher;
using InstituteManagement.Application.Features.Management.Teachers.GetTeachers;
using InstituteManagement.Application.Features.Management.Teachers.UpdateTeacher;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Management.Teachers;

[ApiController]
[Route(ApiRoutes.Catalog.Teachers)]
public sealed class TeachersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTeachersQuery(search, departmentId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(TeacherValuesRequest values, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new CreateTeacherCommand(values), cancellationToken);
        return Created($"/{ApiRoutes.Catalog.Teachers}/{item.Id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, TeacherValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateTeacherCommand(id, values), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new DeleteTeacherCommand(id), cancellationToken) ? NoContent() : NotFound();
}
