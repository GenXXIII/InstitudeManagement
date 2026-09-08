namespace InstituteManagement.API.Contracts.Administration;

public static class SettingsAssetUploadValidator
{
    private const long MaximumFileSize = 5 * 1024 * 1024;

    private static readonly Dictionary<string, HashSet<string>> AssetTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["logo"] = new(["image/png", "image/jpeg", "image/webp", "image/svg+xml"], StringComparer.OrdinalIgnoreCase),
        ["favicon"] = new(["image/x-icon", "image/vnd.microsoft.icon", "image/png", "image/svg+xml"], StringComparer.OrdinalIgnoreCase),
    };

    public static string ValidateAndGetExtension(string kind, IFormFile file)
    {
        if (!AssetTypes.TryGetValue(kind, out var allowedTypes))
            throw new ArgumentException("Only logo and favicon assets can be uploaded.", nameof(kind));
        if (file.Length == 0)
            throw new ArgumentException("Choose a non-empty image file.", nameof(file));
        if (file.Length > MaximumFileSize)
            throw new ArgumentException("The selected image must be 5 MB or smaller.", nameof(file));
        if (!allowedTypes.Contains(file.ContentType))
            throw new ArgumentException($"The selected file type is not supported for the {kind}.", nameof(file));

        return file.ContentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => ".ico",
        };
    }
}
