using InstituteManagement.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/enrollment/{resource}")]
public sealed class EnrollmentController(IEnrollmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string resource, string? search, Guid? departmentId, int? year, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(resource, search, departmentId, year, cancellationToken));

    [HttpPut("{resourceId:guid}")]
    public async Task<IActionResult> Update(string resource, Guid resourceId, Dictionary<string, string> values, CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(resource, resourceId, values, cancellationToken));

    [HttpDelete("{resourceId:guid}")]
    public async Task<IActionResult> Remove(string resource, Guid resourceId, CancellationToken cancellationToken) =>
        await service.RemoveAsync(resource, resourceId, cancellationToken) ? NoContent() : NotFound();
}
