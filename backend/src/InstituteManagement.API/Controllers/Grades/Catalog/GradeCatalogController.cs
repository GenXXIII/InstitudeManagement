using InstituteManagement.API.Contracts.Grades;
using InstituteManagement.Application.Features.Grades.CreateGradeRecord;
using InstituteManagement.Application.Features.Grades.DeleteGradeRecord;
using InstituteManagement.Application.Features.Grades.GetGradeRecords;
using InstituteManagement.Application.Features.Grades.UpdateGradeRecord;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Grades;

[ApiController]
[Route(ApiRoutes.Catalog.Grades)]
public sealed class GradeCatalogController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetGradeRecordsQuery(search, departmentId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(GradeRecordValuesRequest values, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new CreateGradeRecordCommand(values), cancellationToken);
        return Created($"/{ApiRoutes.Catalog.Grades}/{item.Id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, GradeRecordValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateGradeRecordCommand(id, values), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new DeleteGradeRecordCommand(id), cancellationToken) ? NoContent() : NotFound();
}
