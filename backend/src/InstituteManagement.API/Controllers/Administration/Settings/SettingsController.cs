using InstituteManagement.API.Contracts.Administration;
using InstituteManagement.API.Services.Administration;
using InstituteManagement.Application.Features.Administration.GetAllSettings;
using InstituteManagement.Application.Features.Administration.GetSettings;
using InstituteManagement.Application.Features.Administration.SaveSettings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Administration;

[ApiController]
[Route(ApiRoutes.Settings)]
public sealed class SettingsController(ISender sender, SettingsAssetStorage assetStorage) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await sender.Send(new GetAllSettingsQuery(), ct));

    [HttpGet("{section}")]
    public async Task<IActionResult> Get(string section, CancellationToken ct) => Ok(await sender.Send(new GetSettingsQuery(section), ct));

    [HttpPut("{section}")]
    public async Task<IActionResult> Save(string section, [FromBody] SettingsValuesRequest values, CancellationToken ct) =>
        Ok(await sender.Send(new SaveSettingsCommand(section, values), ct));

    [HttpPost("assets/{kind}")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAsset(string kind, IFormFile file, CancellationToken ct)
    {
        var publicOrigin = $"{Request.Scheme}://{Request.Host}";
        return Ok(await assetStorage.SaveAsync(kind, file, publicOrigin, ct));
    }
}
