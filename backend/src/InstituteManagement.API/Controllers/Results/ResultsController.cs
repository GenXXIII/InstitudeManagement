using InstituteManagement.Application.Features.Results.GetResults;
using InstituteManagement.Application.Features.Results;
using InstituteManagement.API.Contracts.Results;
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

    [HttpPost("publish")]
    public async Task<IActionResult> Publish(PublishSemesterResultRequest request, CancellationToken cancellationToken)
    {
        await resultService.PublishAsync(request.StudentId, request.AcademicYear, request.Semester, cancellationToken);
        return NoContent();
    }

    [HttpPost("publish-all")]
    public async Task<IActionResult> PublishAll(Guid? departmentId, int? year, CancellationToken cancellationToken) =>
        Ok(new { published = await resultService.PublishAllAsync(departmentId, year, cancellationToken) });
}
