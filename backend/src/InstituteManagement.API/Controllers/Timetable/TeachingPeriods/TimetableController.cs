using InstituteManagement.Application.Features.Timetable.GetTeachingPeriods;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Timetable;

[ApiController]
[Route(ApiRoutes.Timetable)]
public sealed class TimetableController(ISender sender) : ControllerBase
{
    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTeachingPeriodsQuery(), cancellationToken));
}
