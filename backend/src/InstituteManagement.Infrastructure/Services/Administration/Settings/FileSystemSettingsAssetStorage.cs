using InstituteManagement.Application.Features.Administration.Settings.Assets;
using Microsoft.Extensions.Hosting;

namespace InstituteManagement.Infrastructure.Services.Administration.Settings;

public sealed class FileSystemSettingsAssetStorage(IHostEnvironment environment) : ISettingsAssetStorage
{
    public async Task<string> SaveAsync(
        string kind,
        string extension,
        Stream content,
        CancellationToken cancellationToken)
    {
        var directory = Path.Combine(environment.ContentRootPath, "uploads", "settings");
        Directory.CreateDirectory(directory);

        var storedFileName = $"{kind}-{Guid.NewGuid():N}{extension}";
        await using var output = File.Create(Path.Combine(directory, storedFileName));
        await content.CopyToAsync(output, cancellationToken);

        return $"/uploads/settings/{storedFileName}";
    }
}
