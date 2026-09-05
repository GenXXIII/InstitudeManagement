namespace InstituteManagement.Infrastructure.Services.Notifications.Common;

internal static class NotificationContentValidator
{
    public static string Required(string? value, string label, int maximum)
    {
        var normalized = value?.Trim() ?? "";
        if (normalized.Length == 0) throw new ArgumentException($"{label} is required.");
        if (normalized.Length > maximum) throw new ArgumentException($"{label} must not exceed {maximum} characters.");
        return normalized;
    }

    public static string Code(string? value, string label)
    {
        var normalized = Required(value, label, 64);
        if (normalized.Any(character => !char.IsLetterOrDigit(character) && character is not ('.' or '_' or '/' or '-')))
            throw new ArgumentException($"{label} may use only letters, numbers, dot, underscore, slash, or hyphen.");
        return normalized;
    }

    public static string Choice(string? value, IEnumerable<string> allowed, string label)
    {
        var normalized = allowed.FirstOrDefault(item => item.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return normalized ?? throw new ArgumentException($"{label} is invalid.");
    }

    public static string SeverityFor(string announcementType) => announcementType switch
    {
        "Emergency" => "Critical",
        "Attendance" => "Warning",
        _ => "Info"
    };
}
