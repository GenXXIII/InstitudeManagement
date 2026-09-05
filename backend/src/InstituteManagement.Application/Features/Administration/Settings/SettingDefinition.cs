namespace InstituteManagement.Application.Features.Administration.Settings;

public enum SettingValueType
{
    Text,
    Boolean,
    Integer,
    Decimal,
    Date,
    Email,
    Uri,
    TimeZone,
    Option,
    Code,
    Digits,
    Path,
    UtcOffset,
    List
}

public sealed record SettingDefinition(
    string Key,
    string DefaultValue,
    SettingValueType ValueType = SettingValueType.Text,
    decimal? Minimum = null,
    decimal? Maximum = null,
    int MaximumLength = 2048,
    bool AllowEmpty = false,
    IReadOnlyList<string>? Options = null);

public sealed class SettingsSectionDefinition(string name, params SettingDefinition[] settings)
{
    public string Name { get; } = name;
    public IReadOnlyList<SettingDefinition> Settings { get; } = settings;
    public IReadOnlyDictionary<string, SettingDefinition> SettingsByKey { get; } =
        settings.ToDictionary(setting => setting.Key, StringComparer.Ordinal);
}
