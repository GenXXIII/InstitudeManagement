using InstituteManagement.API.Contracts.Management.Departments;
using InstituteManagement.Application.Features.Management.Departments.CreateDepartment;
using InstituteManagement.Application.Features.Management.Departments.DeleteDepartment;
using InstituteManagement.Application.Features.Management.Departments.GetDepartments;
using InstituteManagement.Application.Features.Management.Departments.UpdateDepartment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Management.Departments;

[ApiController]
[Route(ApiRoutes.Catalog.Departments)]
public sealed class DepartmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetDepartmentsQuery(search, departmentId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(DepartmentValuesRequest values, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new CreateDepartmentCommand(values), cancellationToken);
        return Created($"/{ApiRoutes.Catalog.Departments}/{item.Id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, DepartmentValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateDepartmentCommand(id, values), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new DeleteDepartmentCommand(id), cancellationToken) ? NoContent() : NotFound();
}
