using InstituteManagement.API.Contracts.Record.Sessions;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Record.UpdateClassSessionRecord;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Record.Sessions;

[ApiController]
[Route(ApiRoutes.OperationalRecords)]
public sealed class ClassSessionRecordsController(ISender sender) : ControllerBase
{
    [HttpPut("sessions/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateClassSessionRecordRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateClassSessionRecordCommand(id, request.ToDto()), cancellationToken);
        return NoContent();
    }
}
