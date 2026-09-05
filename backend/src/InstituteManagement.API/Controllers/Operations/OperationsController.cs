using InstituteManagement.Application.Features.Operations.GetOperation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Operations;

[ApiController]
[Route(ApiRoutes.Operations)]
public sealed class OperationsController(ISender sender) : ControllerBase
{
    [HttpGet("{module}")]
    public async Task<IActionResult> Get(string module, [FromQuery] Guid? departmentId, CancellationToken ct) =>
        Ok(await sender.Send(new GetOperationQuery(module, departmentId), ct));
}
