using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.History.GetHistory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.History;

[ApiController]
[Route(ApiRoutes.History)]
public sealed class HistoryController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, string? type, int page = 1, int pageSize = 40, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetHistoryQuery(search, type, new PageRequest(page, pageSize)), cancellationToken));
}
