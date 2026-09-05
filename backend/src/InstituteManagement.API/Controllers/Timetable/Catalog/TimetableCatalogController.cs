using InstituteManagement.API.Contracts.Timetable;
using InstituteManagement.Application.Features.Timetable.CreateTimetableEntry;
using InstituteManagement.Application.Features.Timetable.DeleteTimetableEntry;
using InstituteManagement.Application.Features.Timetable.GetTimetableEntries;
using InstituteManagement.Application.Features.Timetable.UpdateTimetableEntry;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Timetable;

[ApiController]
[Route(ApiRoutes.Catalog.Timetable)]
public sealed class TimetableCatalogController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTimetableEntriesQuery(search, departmentId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(TimetableEntryValuesRequest values, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new CreateTimetableEntryCommand(values), cancellationToken);
        return Created($"/{ApiRoutes.Catalog.Timetable}/{item.Id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, TimetableEntryValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateTimetableEntryCommand(id, values), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new DeleteTimetableEntryCommand(id), cancellationToken) ? NoContent() : NotFound();
}
