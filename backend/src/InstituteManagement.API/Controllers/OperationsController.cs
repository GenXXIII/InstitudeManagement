using InstituteManagement.Application.Features.Operations.GetOperation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/operations")]
public sealed class OperationsController(ISender sender) : ControllerBase
{
    [HttpGet("{module}")]
    public async Task<IActionResult> Get(string module, [FromQuery] Guid? departmentId, CancellationToken ct) =>
        Ok(await sender.Send(new GetOperationQuery(module, departmentId), ct));
}
