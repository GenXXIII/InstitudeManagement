using InstituteManagement.Application.Features.Administration.GetAllSettings;
using InstituteManagement.Application.Features.Administration.GetSettings;
using InstituteManagement.Application.Features.Administration.SaveSettings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(ISender sender, IWebHostEnvironment environment) : ControllerBase
{
    private static readonly Dictionary<string, HashSet<string>> AssetTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["logo"] = new(["image/png", "image/jpeg", "image/webp", "image/svg+xml"], StringComparer.OrdinalIgnoreCase),
        ["favicon"] = new(["image/x-icon", "image/vnd.microsoft.icon", "image/png", "image/svg+xml"], StringComparer.OrdinalIgnoreCase),
    };

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await sender.Send(new GetAllSettingsQuery(), ct));

    [HttpGet("{section}")]
    public async Task<IActionResult> Get(string section, CancellationToken ct) => Ok(await sender.Send(new GetSettingsQuery(section), ct));

    [HttpPut("{section}")]
    public async Task<IActionResult> Save(string section, [FromBody] Dictionary<string, string> values, CancellationToken ct) =>
        Ok(await sender.Send(new SaveSettingsCommand(section, values), ct));

    [HttpPost("assets/{kind}")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAsset(string kind, IFormFile file, CancellationToken ct)
    {
        if (!AssetTypes.TryGetValue(kind, out var allowedTypes)) return BadRequest(new { detail = "Only logo and favicon assets can be uploaded." });
        if (file.Length == 0) return BadRequest(new { detail = "Choose a non-empty image file." });
        if (file.Length > 5 * 1024 * 1024) return BadRequest(new { detail = "The selected image must be 5 MB or smaller." });
        if (!allowedTypes.Contains(file.ContentType)) return BadRequest(new { detail = $"The selected file type is not supported for the {kind}." });

        var extension = file.ContentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => ".ico",
        };
        var directory = Path.Combine(environment.ContentRootPath, "uploads", "settings");
        Directory.CreateDirectory(directory);
        var fileName = $"{kind}-{Guid.NewGuid():N}{extension}";
        await using var output = System.IO.File.Create(Path.Combine(directory, fileName));
        await file.CopyToAsync(output, ct);

        var relativePath = $"/uploads/settings/{fileName}";
        var publicUrl = $"{Request.Scheme}://{Request.Host}{relativePath}";
        return Ok(new { url = publicUrl, path = relativePath, fileName = file.FileName });
    }
}
