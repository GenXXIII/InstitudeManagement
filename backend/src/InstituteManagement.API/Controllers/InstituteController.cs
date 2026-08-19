using InstituteManagement.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api")]
public sealed class InstituteController(ISender sender) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct) => Ok(await sender.Send(new GetDashboardQuery(), ct));

    [HttpGet("operations/{module}")]
    public async Task<IActionResult> Operation(string module, [FromQuery] Guid? departmentId, CancellationToken ct) => Ok(await sender.Send(new GetOperationQuery(module, departmentId), ct));

    [HttpGet("records")]
    public async Task<IActionResult> Records([FromQuery] string? search, [FromQuery] string? type, CancellationToken ct) => Ok(await sender.Send(new GetRecordsQuery(search, type), ct));

    [HttpGet("catalog/{resource}")]
    public async Task<IActionResult> Catalog(string resource, [FromQuery] string? search, [FromQuery] Guid? departmentId, CancellationToken ct) => Ok(await sender.Send(new GetCatalogQuery(resource, search, departmentId), ct));

    [HttpPost("catalog/{resource}")]
    public async Task<IActionResult> Create(string resource, [FromBody] Dictionary<string, string> values, CancellationToken ct)
    {
        var created = await sender.Send(new CreateCatalogCommand(resource, values), ct);
        return Created($"/api/catalog/{resource}/{created.Id}", created);
    }

    [HttpPut("catalog/{resource}/{id:guid}")]
    public async Task<IActionResult> Update(string resource, Guid id, [FromBody] Dictionary<string, string> values, CancellationToken ct) =>
        Ok(await sender.Send(new UpdateCatalogCommand(resource, id, values), ct));

    [HttpDelete("catalog/{resource}/{id:guid}")]
    public async Task<IActionResult> Delete(string resource, Guid id, CancellationToken ct) =>
        await sender.Send(new DeleteCatalogCommand(resource, id), ct) ? NoContent() : NotFound();

    [HttpGet("settings/{section}")]
    public async Task<IActionResult> Settings(string section, CancellationToken ct) => Ok(await sender.Send(new GetSettingsQuery(section), ct));

    [HttpPut("settings/{section}")]
    public async Task<IActionResult> SaveSettings(string section, [FromBody] Dictionary<string, string> values, CancellationToken ct) => Ok(await sender.Send(new SaveSettingsCommand(section, values), ct));

    [HttpPost("attendance")]
    public async Task<IActionResult> RecordAttendance([FromBody] AttendanceRequest request, CancellationToken ct)
    {
        await sender.Send(new RecordAttendanceCommand(request.StudentId, request.Status), ct);
        return Accepted();
    }

    [HttpPost("grades")]
    public async Task<IActionResult> SubmitGrade([FromBody] GradeRequest request, CancellationToken ct)
    {
        await sender.Send(new SubmitGradeCommand(request.StudentId, request.CourseId, request.Score), ct);
        return Accepted();
    }
}

public sealed record AttendanceRequest(Guid StudentId, string Status);
public sealed record GradeRequest(Guid StudentId, Guid CourseId, decimal Score);
