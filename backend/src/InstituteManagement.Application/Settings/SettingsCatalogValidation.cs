using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace InstituteManagement.Application.Settings;

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

    private static void ValidateCrossFields(string section, IReadOnlyDictionary<string, string> values, ICollection<string> errors)
    {
        if (section == "academic-year")
        {
            ValidateWindow(values, "startsOn", "endsOn", "Academic year", errors);
        }
        else if (section == "semester")
        {
            if (!TryWindow(values, "semester1StartsOn", "semester1EndsOn", "Semester 1", errors, out var semester1Start, out var semester1End)
                || !TryWindow(values, "semester2StartsOn", "semester2EndsOn", "Semester 2", errors, out var semester2Start, out var semester2End)
                || !TryWindow(values, "summerStartsOn", "summerEndsOn", "Summer Term", errors, out var summerStart, out var summerEnd)) return;
            if (!(semester1End < semester2Start && semester2End < summerStart)) errors.Add("Term date windows must be ordered Semester 1, Semester 2, then Summer Term without overlap.");
            var selected = values["currentTerm"] switch
            {
                "Semester 1" => (semester1Start, semester1End),
                "Semester 2" => (semester2Start, semester2End),
                _ => (summerStart, summerEnd)
            };
            if (Date(values["startsOn"]) != selected.Item1 || Date(values["endsOn"]) != selected.Item2)
                errors.Add("startsOn and endsOn must match the selected currentTerm window.");
        }
        else if (section == "attendance-rules")
        {
            var onTime = Integer(values["onTimeThresholdMinutes"]);
            var lateThreshold = Integer(values["lateThresholdMinutes"]);
            var veryLate = Integer(values["veryLateThresholdMinutes"]);
            if (!(onTime < lateThreshold && lateThreshold < veryLate))
                errors.Add("Attendance thresholds must progress from On Time to Late to Very Late.");
        }
        else if (section == "grade-rules")
        {
            var minimum = Decimal(values["minimumScore"]);
            var maximum = Decimal(values["maximumScore"]);
            var thresholds = new[] { "aPlusMinimum", "aMinimum", "bPlusMinimum", "bMinimum", "cPlusMinimum", "cMinimum", "dMinimum" }.Select(key => Decimal(values[key])).ToArray();
            if (minimum >= maximum) errors.Add("minimumScore must be lower than maximumScore.");
            if (!(thresholds[0] <= maximum && thresholds.Zip(thresholds.Skip(1)).All(pair => pair.First > pair.Second) && thresholds[^1] >= minimum))
                errors.Add("Grade thresholds must descend A+, A, B+, B, C+, C, D; lower scores are F.");
            foreach (var key in new[] { "passMark", "overallPassMark", "coursePassMark", "finalExamMinimum" })
                if (Decimal(values[key]) < minimum || Decimal(values[key]) > maximum) errors.Add($"{key} must be within the score range.");
            var maximumGpa = Decimal(values["maximumGpa"]);
            foreach (var key in new[] { "aPlusGpa", "aGpa", "bPlusGpa", "bGpa", "cPlusGpa", "cGpa", "dGpa", "fGpa", "gpaScale" })
                if (Decimal(values[key]) > maximumGpa) errors.Add($"{key} cannot exceed maximumGpa.");
        }
        else if (section == "notifications")
        {
            if (bool.TryParse(values["emailEnabled"], out var emailEnabled) && emailEnabled)
            {
                foreach (var key in new[] { "smtpHost", "smtpPort", "emailEncryption", "senderName", "senderEmail" })
                    if (string.IsNullOrWhiteSpace(values[key])) errors.Add($"{key} is required while email is enabled.");
            }
            if (bool.TryParse(values["smsEnabled"], out var smsEnabled) && smsEnabled && values["smsProvider"] == "None")
                errors.Add("smsProvider must be configured while SMS is enabled.");
        }
    }

    private static void ValidateWindow(IReadOnlyDictionary<string, string> values, string startKey, string endKey, string label, ICollection<string> errors) =>
        TryWindow(values, startKey, endKey, label, errors, out _, out _);

    private static bool TryWindow(IReadOnlyDictionary<string, string> values, string startKey, string endKey, string label, ICollection<string> errors, out DateOnly start, out DateOnly end)
    {
        var validStart = DateOnly.TryParseExact(values.GetValueOrDefault(startKey), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start);
        var validEnd = DateOnly.TryParseExact(values.GetValueOrDefault(endKey), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out end);
        if (!validStart || !validEnd) return false;
        if (end <= start) { errors.Add($"{label} end date must be after its start date."); return false; }
        return true;
    }

    private static DateOnly Date(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static int Integer(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    private static decimal Decimal(string value) => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static bool IsAssetLocation(string value) =>
        value.StartsWith("/", StringComparison.Ordinal)
        || (System.Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https");
}
