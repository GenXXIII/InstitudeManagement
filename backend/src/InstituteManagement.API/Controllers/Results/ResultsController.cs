using InstituteManagement.Application.Features.Results.GetResults;
using InstituteManagement.Application.Features.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Results;

[ApiController]
[Route(ApiRoutes.Results)]
public sealed class ResultsController(ISender sender, IResultQueryService resultService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, Guid? studentId, bool publishedOnly, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetResultsQuery(departmentId, year, semester, academicYear, history, studentId, publishedOnly), cancellationToken));

    [HttpPost("publish-all")]
    public async Task<IActionResult> PublishAll(CancellationToken cancellationToken) =>
        Ok(new { published = await resultService.PublishAllAsync(cancellationToken) });
}
