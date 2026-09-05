namespace InstituteManagement.Application.Features.Administration;

public sealed record SettingsDto(
    string Section,
    Dictionary<string, string> Values,
    bool IsConfigured,
    DateTime? UpdatedAtUtc);
