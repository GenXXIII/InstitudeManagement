using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Record.GetOperationalRecords;
using InstituteManagement.Application.Common.Pagination;
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
        int? year,
        string? period,
        bool history,
        int page = 1,
        int pageSize = 40,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(
            new GetOperationalRecordsQuery(module, search, departmentId, year, period, history, new PageRequest(page, pageSize)),
            cancellationToken));
}
