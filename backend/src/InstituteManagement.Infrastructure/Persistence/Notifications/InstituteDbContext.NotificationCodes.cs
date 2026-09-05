using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public sealed partial class InstituteDbContext
{
    private void AssignSourceBusinessCodes(NotificationCodeFormat? format)
    {
        if (format is null) return;
        var notifications = ChangeTracker.Entries<Notification>().Where(entry => entry.State == EntityState.Added).OrderBy(entry => entry.Entity.CreateAt).ThenBy(entry => entry.Entity.Id).ToList();
        if (notifications.Count > 0)
        {
            var stem = format.Stem(format.NotificationPrefix);
            var next = NextSequence(Notifications.AsNoTracking().Select(item => item.NotificationCode).ToList(), stem, format.StartingNumber);
            foreach (var entry in notifications) entry.Entity.NotificationCode = BusinessCode(stem, next++, format.PaddingWidth);
        }

    }

    private void AssignHistoryBusinessCodes(NotificationCodeFormat? format)
    {
        if (format is null) return;
        var entries = ChangeTracker.Entries<NotificationHistory>().Where(entry => entry.State == EntityState.Added).OrderBy(entry => entry.Entity.CreateAt).ThenBy(entry => entry.Entity.Id).ToList();
        if (entries.Count == 0) return;
        var stem = format.Stem(format.HistoryPrefix);
        var next = NextSequence(NotificationHistory.AsNoTracking().Select(item => item.NotificationHistoryCode).ToList(), stem, format.StartingNumber);
        foreach (var entry in entries) entry.Entity.NotificationHistoryCode = BusinessCode(stem, next++, format.PaddingWidth);
    }

    private bool RequiresNotificationCodeFormat() =>
        ChangeTracker.Entries<Notification>().Any(entry => entry.State == EntityState.Added)
        || ChangeTracker.Entries<NotificationHistory>().Any(entry => entry.State == EntityState.Added);

    private NotificationCodeFormat LoadNotificationCodeFormat()
    {
        var values = SystemSettings.AsNoTracking().Where(setting => setting.Section == "notifications")
            .ToDictionary(setting => setting.Key, setting => setting.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in ChangeTracker.Entries<SystemSetting>().Where(entry =>
                     entry.State is EntityState.Added or EntityState.Modified
                     && entry.Entity.Section.Equals("notifications", StringComparison.OrdinalIgnoreCase)))
            values[entry.Entity.Key] = entry.Entity.Value;

        var timeZoneId = SystemSettings.AsNoTracking().Where(setting => setting.Section == "system" && setting.Key == "timeZone").Select(setting => setting.Value).FirstOrDefault() ?? "Asia/Phnom_Penh";
        var pendingTimeZone = ChangeTracker.Entries<SystemSetting>().FirstOrDefault(entry =>
            entry.State is EntityState.Added or EntityState.Modified
            && entry.Entity.Section.Equals("system", StringComparison.OrdinalIgnoreCase)
            && entry.Entity.Key.Equals("timeZone", StringComparison.OrdinalIgnoreCase));
        if (pendingTimeZone is not null) timeZoneId = pendingTimeZone.Entity.Value;
        var localNow = DateTime.UtcNow;
        try { localNow = TimeZoneInfo.ConvertTimeFromUtc(localNow, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)); }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }

        var separator = values.GetValueOrDefault("codeSeparator", "-");
        var width = int.TryParse(values.GetValueOrDefault("codePaddingWidth"), out var configuredWidth) ? Math.Clamp(configuredWidth, 1, 12) : 8;
        var start = long.TryParse(values.GetValueOrDefault("codeStartingNumber"), out var configuredStart) && configuredStart >= 0 ? configuredStart : 1;
        var includeYear = bool.TryParse(values.GetValueOrDefault("codeIncludeYear"), out var configuredIncludeYear) && configuredIncludeYear;
        return new(
            Prefix(values, "notificationCodePrefix", "NOT"),
            Prefix(values, "historyCodePrefix", "NHS"),
            separator,
            includeYear,
            localNow.Year,
            start,
            width);
    }

    private static string Prefix(IReadOnlyDictionary<string, string> values, string key, string fallback) =>
        string.IsNullOrWhiteSpace(values.GetValueOrDefault(key)) ? fallback : values[key].Trim().ToUpperInvariant();

    private static long NextSequence(IEnumerable<string> codes, string stem, long startingNumber) => codes
        .Where(code => code.StartsWith(stem, StringComparison.OrdinalIgnoreCase))
        .Select(code => long.TryParse(code[stem.Length..], out var number) ? number : startingNumber - 1)
        .DefaultIfEmpty(startingNumber - 1)
        .Max() + 1;

    private static string BusinessCode(string stem, long sequence, int paddingWidth) => $"{stem}{sequence.ToString().PadLeft(paddingWidth, '0')}";

    private sealed record NotificationCodeFormat(string NotificationPrefix, string HistoryPrefix, string Separator, bool IncludeYear, int Year, long StartingNumber, int PaddingWidth)
    {
        public string Stem(string prefix) => IncludeYear ? $"{prefix}{Separator}{Year}{Separator}" : $"{prefix}{Separator}";
    }
}
