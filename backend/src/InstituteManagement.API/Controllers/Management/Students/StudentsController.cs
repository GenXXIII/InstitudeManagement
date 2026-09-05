using InstituteManagement.API.Contracts.Management.Students;
using InstituteManagement.Application.Features.Management.Students.CreateStudent;
using InstituteManagement.Application.Features.Management.Students.DeleteStudent;
using InstituteManagement.Application.Features.Management.Students.GetStudents;
using InstituteManagement.Application.Features.Management.Students.UpdateStudent;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Management.Students;

[ApiController]
[Route(ApiRoutes.Catalog.Students)]
public sealed class StudentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStudentsQuery(search, departmentId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(StudentValuesRequest values, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new CreateStudentCommand(values), cancellationToken);
        return Created($"/{ApiRoutes.Catalog.Students}/{item.Id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, StudentValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateStudentCommand(id, values), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new DeleteStudentCommand(id), cancellationToken) ? NoContent() : NotFound();
}
