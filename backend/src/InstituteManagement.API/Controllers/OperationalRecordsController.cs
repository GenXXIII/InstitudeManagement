using InstituteManagement.Application.Features.Record.GetOperationalRecords;
using InstituteManagement.Application.Features.Record.UpdateClassSessionRecord;
using InstituteManagement.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/operational-records")]
public sealed class OperationalRecordsController(ISender sender) : ControllerBase
{
    [HttpGet("{module}")]
    public async Task<IActionResult> Get(string module, string? search, Guid? departmentId, bool history, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetOperationalRecordsQuery(module, search, departmentId, history), cancellationToken));

    [HttpPut("sessions/{id:guid}")]
    public async Task<IActionResult> UpdateSession(Guid id, UpdateClassSessionRecordDto update, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateClassSessionRecordCommand(id, update), cancellationToken);
        return NoContent();
    }
}
