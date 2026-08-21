using InstituteManagement.Application.Features.Timetable.GetTeachingPeriods;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/timetable")]
public sealed class TimetableController(ISender sender) : ControllerBase
{
    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTeachingPeriodsQuery(), cancellationToken));
}
