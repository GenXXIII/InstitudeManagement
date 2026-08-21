using InstituteManagement.Application.Features.History.GetHistory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/records")]
public sealed class HistoryController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, string? type, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetHistoryQuery(search, type), cancellationToken));
}
