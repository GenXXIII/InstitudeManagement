using InstituteManagement.Application.Features.Administration.GetSettings;
using InstituteManagement.Application.Features.Administration.SaveSettings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(ISender sender) : ControllerBase
{
    [HttpGet("{section}")]
    public async Task<IActionResult> Get(string section, CancellationToken ct) => Ok(await sender.Send(new GetSettingsQuery(section), ct));

    [HttpPut("{section}")]
    public async Task<IActionResult> Save(string section, [FromBody] Dictionary<string, string> values, CancellationToken ct) =>
        Ok(await sender.Send(new SaveSettingsCommand(section, values), ct));
}
