using InstituteManagement.Application.Features.Results.GetResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/results")]
public sealed class ResultsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetResultsQuery(departmentId, year, semester, academicYear, history), cancellationToken));
}
