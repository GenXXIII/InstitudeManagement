using InstituteManagement.API.Contracts.Administration;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Administration.GetAllSettings;
using InstituteManagement.Application.Features.Administration.GetSettings;
using InstituteManagement.Application.Features.Administration.SaveSettings;
using InstituteManagement.Application.Features.Administration.Settings.Assets;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Administration;

[ApiController]
[Route(ApiRoutes.Settings)]
public sealed class SettingsController(ISender sender, ISettingsAssetStorage assetStorage) : ControllerBase
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
        var extension = SettingsAssetUploadValidator.ValidateAndGetExtension(kind, file);
        await using var content = file.OpenReadStream();
        var relativePath = await assetStorage.SaveAsync(kind, extension, content, ct);
        var publicOrigin = $"{Request.Scheme}://{Request.Host}";
        return Ok(new SettingsAssetResponse(
            $"{publicOrigin}{relativePath}",
            relativePath,
            file.FileName));
    }
}
