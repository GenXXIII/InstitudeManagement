using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace InstituteManagement.Application.Features.Administration.Settings;

public static partial class SettingsCatalog
{
    private static readonly Regex CodePattern = new("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant);
    private static readonly Regex DigitsPattern = new("^[0-9]+$", RegexOptions.CultureInvariant);
    private static readonly Regex UtcOffsetPattern = new("^UTC[+-](?:0[0-9]|1[0-4]):[0-5][0-9]$", RegexOptions.CultureInvariant);

    public static Dictionary<string, string> MergeDefaults(string section, IEnumerable<KeyValuePair<string, string>> storedValues)
    {
        var definition = GetSection(section);
        var merged = definition.Settings.ToDictionary(setting => setting.Key, setting => setting.DefaultValue, StringComparer.Ordinal);
        foreach (var item in storedValues)
            if (definition.SettingsByKey.ContainsKey(item.Key)) merged[item.Key] = item.Value;
        return merged;
    }

    public static bool IsConfigured(string section, IEnumerable<string> storedKeys)
    {
        var keys = storedKeys.ToHashSet(StringComparer.Ordinal);
        return GetSection(section).Settings.All(setting => keys.Contains(setting.Key));
    }

    public static Dictionary<string, string> NormalizeAndValidate(string section, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var definition = GetSection(section);
        var unknown = values.Keys.Where(key => !definition.SettingsByKey.ContainsKey(key)).Order().ToArray();
        if (unknown.Length > 0) throw new ArgumentException($"Unknown settings for {definition.Name}: {string.Join(", ", unknown)}.");
        var missing = definition.Settings.Select(setting => setting.Key).Where(key => !values.ContainsKey(key)).ToArray();
        if (missing.Length > 0) throw new ArgumentException($"Missing settings for {definition.Name}: {string.Join(", ", missing)}.");

        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
        var errors = new List<string>();
        foreach (var setting in definition.Settings)
        {
            var raw = values[setting.Key];
            if (raw is null)
            {
                errors.Add($"{setting.Key} requires a value.");
                continue;
            }

            var value = raw.Trim();
            if (!setting.AllowEmpty && value.Length == 0) errors.Add($"{setting.Key} is required.");
            else if (value.Length > setting.MaximumLength) errors.Add($"{setting.Key} must contain no more than {setting.MaximumLength} characters.");
            else if (value.Length > 0) ValidateValue(setting, value, errors);
            normalized[setting.Key] = NormalizeValue(setting, value);
        }

        ValidateCrossFields(definition.Name, normalized, errors);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        return normalized;
    }

    private static void ValidateValue(SettingDefinition setting, string value, ICollection<string> errors)
    {
        switch (setting.ValueType)
        {
            case SettingValueType.Boolean when !bool.TryParse(value, out _):
                errors.Add($"{setting.Key} must be true or false.");
                break;
            case SettingValueType.Integer:
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer)
                    || integer < setting.Minimum || integer > setting.Maximum)
                    errors.Add($"{setting.Key} must be between {setting.Minimum} and {setting.Maximum}.");
                break;
            case SettingValueType.Decimal:
                if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
                    || number < setting.Minimum || number > setting.Maximum)
                    errors.Add($"{setting.Key} must be between {setting.Minimum} and {setting.Maximum}.");
                break;
            case SettingValueType.Date when !DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _):
                errors.Add($"{setting.Key} must use yyyy-MM-dd.");
                break;
            case SettingValueType.Email when !MailAddress.TryCreate(value, out _):
                errors.Add($"{setting.Key} must be a valid email address.");
                break;
            case SettingValueType.Uri when (!System.Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")):
                errors.Add($"{setting.Key} must be an absolute HTTP or HTTPS URL.");
                break;
            case SettingValueType.TimeZone:
                try { _ = TimeZoneInfo.FindSystemTimeZoneById(value); }
                catch (TimeZoneNotFoundException) { errors.Add($"{setting.Key} is not a recognized time zone."); }
                catch (InvalidTimeZoneException) { errors.Add($"{setting.Key} is not a valid time zone."); }
                break;
            case SettingValueType.Option when setting.Options?.Contains(value, StringComparer.Ordinal) != true:
                errors.Add($"{setting.Key} must be one of: {string.Join(", ", setting.Options ?? [])}.");
                break;
            case SettingValueType.Code when !CodePattern.IsMatch(value):
                errors.Add($"{setting.Key} may contain only letters, numbers, underscores, and hyphens.");
                break;
            case SettingValueType.Digits when !DigitsPattern.IsMatch(value):
                errors.Add($"{setting.Key} must contain digits only.");
                break;
            case SettingValueType.Path when !IsAssetLocation(value):
                errors.Add($"{setting.Key} must be an application path or an absolute HTTP or HTTPS URL.");
                break;
            case SettingValueType.UtcOffset when !UtcOffsetPattern.IsMatch(value):
                errors.Add($"{setting.Key} must use a value such as UTC+07:00.");
                break;
            case SettingValueType.List:
                var items = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (items.Length == 0 || items.Any(item => item.Length > 128) || items.Distinct(StringComparer.OrdinalIgnoreCase).Count() != items.Length)
                    errors.Add($"{setting.Key} must be a comma-separated list of unique values.");
                break;
        }
    }

    private static string NormalizeValue(SettingDefinition setting, string value) => setting.ValueType switch
    {
        SettingValueType.Boolean when bool.TryParse(value, out var boolean) => boolean ? "true" : "false",
        SettingValueType.Integer when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) => integer.ToString(CultureInfo.InvariantCulture),
        SettingValueType.List => string.Join(',', value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)),
        _ => value
    };

    private static bool IsAssetLocation(string value) =>
        value.StartsWith("/", StringComparison.Ordinal)
        || (System.Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https");
}
