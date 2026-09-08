namespace InstituteManagement.Application.Features.Administration.Settings.Assets;

public interface ISettingsAssetStorage
{
    Task<string> SaveAsync(
        string kind,
        string extension,
        Stream content,
        CancellationToken cancellationToken);
}
