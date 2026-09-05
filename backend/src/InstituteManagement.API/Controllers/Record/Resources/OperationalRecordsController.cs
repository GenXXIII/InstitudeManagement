using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Record.GetOperationalRecords;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Record.Resources;

[ApiController]
[Route(ApiRoutes.OperationalRecords)]
public sealed class OperationalRecordsController(ISender sender) : ControllerBase
{
    [HttpGet("{module}")]
    public async Task<IActionResult> Get(
        string module,
        string? search,
        Guid? departmentId,
        bool history,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new GetOperationalRecordsQuery(module, search, departmentId, history),
            cancellationToken));
}
