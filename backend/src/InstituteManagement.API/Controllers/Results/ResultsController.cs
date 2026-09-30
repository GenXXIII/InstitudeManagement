using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Results.GetResults;
using InstituteManagement.Application.Features.Results.PublishResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Results;

[ApiController]
[Route(ApiRoutes.Results)]
public sealed class ResultsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, Guid? studentId, bool publishedOnly, string? search, string? outcome, int page = 1, int pageSize = 40, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetResultsQuery(departmentId, year, semester, academicYear, history, studentId, publishedOnly, search, outcome, new PageRequest(page, pageSize)), cancellationToken));

    [HttpGet("publication-readiness")]
    public async Task<IActionResult> GetPublicationReadiness(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetResultPublicationReadinessQuery(), cancellationToken));

    [HttpPost("publish-all")]
    public async Task<IActionResult> PublishAll(CancellationToken cancellationToken) =>
        Ok(new { published = await sender.Send(new PublishReadySemesterResultsCommand(), cancellationToken) });
}
