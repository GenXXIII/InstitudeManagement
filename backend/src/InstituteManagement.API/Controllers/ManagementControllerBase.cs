using InstituteManagement.Application.Features.Management.CreateItem;
using InstituteManagement.Application.Features.Management.DeleteItem;
using InstituteManagement.Application.Features.Management.GetItems;
using InstituteManagement.Application.Features.Management.UpdateItem;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
public abstract class ManagementControllerBase(ISender sender, string resource) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetManagementItemsQuery(resource, search, departmentId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new CreateManagementItemCommand(resource, values), cancellationToken);
        return Created($"/api/catalog/{resource}/{item.Id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateManagementItemCommand(resource, id, values), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new DeleteManagementItemCommand(resource, id), cancellationToken) ? NoContent() : NotFound();
}
