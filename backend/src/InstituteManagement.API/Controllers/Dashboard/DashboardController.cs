using InstituteManagement.Application.Features.Dashboard.GetDashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Dashboard;

[ApiController]
[Route(ApiRoutes.Dashboard)]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string range = "monthly", CancellationToken ct = default) => Ok(await sender.Send(new GetDashboardQuery(range), ct));
}
