using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs.Notifications;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Notifications;

public sealed class NotificationCenterService(InstituteDbContext db, InstituteCache cache) : INotificationCenterService
{
    private static readonly HashSet<string> AnnouncementTypes = ["General", "Attendance", "Emergency", "Result"];
    private static readonly HashSet<string> Severities = ["Info", "Warning", "Critical"];

    public async Task<IReadOnlyList<NotificationItemDto>> GetNotificationsAsync(CancellationToken cancellationToken) =>
        await db.Notifications.AsNoTracking().Where(item => !item.IsRead).OrderByDescending(item => item.CreateAt)
            .Select(item => new NotificationItemDto(item.Id, item.NotificationCode, item.Type, item.Title, item.Message, item.Severity, item.IsRead, item.CreateAt))
            .ToListAsync(cancellationToken);

    public async Task<NotificationItemDto> GetNotificationAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Notifications.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Notification not found.");
        return Notification(entity);
    }

    public async Task<NotificationItemDto> MarkNotificationReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Notifications.FindAsync([id], cancellationToken) ?? throw new KeyNotFoundException("Notification not found.");
        if (!entity.IsRead)
        {
            entity.IsRead = true;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await cache.InvalidateDashboardAsync(cancellationToken);
        }
        return Notification(entity);
    }

    public async Task<NotificationItemDto> UpdateNotificationAsync(Guid id, UpdateNotificationRequestDto request, CancellationToken cancellationToken)
    {
        var entity = await db.Notifications.FindAsync([id], cancellationToken) ?? throw new KeyNotFoundException("Notification not found.");
        entity.Title = Required(request.Title, "Notification title", 200);
        entity.Message = Required(request.Message, "Notification detail", 2000);
        entity.Severity = Choice(request.Severity, Severities, "Notification severity");
        entity.IsRead = request.IsRead;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
        return Notification(entity);
    }

    public async Task DeleteNotificationAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Notifications.FindAsync([id], cancellationToken) ?? throw new KeyNotFoundException("Notification not found.");
        var announcement = await db.Announcements.SingleOrDefaultAsync(item => item.NotificationId == id, cancellationToken);
        if (announcement is not null) announcement.NotificationId = null;
        db.Notifications.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnnouncementItemDto>> GetAnnouncementsAsync(CancellationToken cancellationToken) =>
        await db.Announcements.AsNoTracking().Where(item => item.IsActive).OrderByDescending(item => item.CreateAt)
            .Select(item => new AnnouncementItemDto(item.Id, item.AnnouncementCode, item.NotificationId, item.Type, item.Title, item.Message, item.CreateAt))
            .ToListAsync(cancellationToken);

    public async Task<AnnouncementItemDto> CreateAnnouncementAsync(AnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        var type = await ValidateAnnouncementAsync(request, cancellationToken);
        var title = Required(request.Title, "Alert title", 200);
        var message = Required(request.Message, "Alert detail", 2000);
        var entity = new Announcement { Type = type, Title = title, Message = message, Notification = CreateNotification(type, title, message) };
        db.Announcements.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
        return Announcement(entity);
    }

    public async Task<AnnouncementItemDto> UpdateAnnouncementAsync(Guid id, AnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        var entity = await db.Announcements.Include(item => item.Notification).SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Alert not found.");
        entity.Type = await ValidateAnnouncementAsync(request, cancellationToken);
        entity.Title = Required(request.Title, "Alert title", 200);
        entity.Message = Required(request.Message, "Alert detail", 2000);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        entity.Notification ??= CreateNotification(entity.Type, entity.Title, entity.Message);
        entity.Notification.Title = entity.Title;
        entity.Notification.Message = entity.Message;
        entity.Notification.Type = entity.Type;
        entity.Notification.Severity = Severity(entity.Type);
        entity.Notification.IsRead = false;
        entity.Notification.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
        return Announcement(entity);
    }

    public async Task DeleteAnnouncementAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Announcements.Include(item => item.Notification).SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Alert not found.");
        var notification = entity.Notification;
        db.Announcements.Remove(entity);
        if (notification is not null) db.Notifications.Remove(notification);
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationHistoryItemDto>> GetHistoryAsync(CancellationToken cancellationToken) =>
        await db.NotificationHistory.AsNoTracking().Where(item => item.Kind == "Notification" && item.Action == "Read").OrderByDescending(item => item.CreateAt)
            .Select(item => new NotificationHistoryItemDto(item.Id, item.NotificationHistoryCode, item.SourceId, item.SourceCode, item.Kind, item.Type, item.Title, item.Message, item.Action, item.CreateAt))
            .ToListAsync(cancellationToken);

    public async Task<NotificationHistoryItemDto> GetHistoryItemAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.NotificationHistory.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Notification history not found.");
        return History(item);
    }

    private async Task<string> ValidateAnnouncementAsync(AnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        var type = Choice(request.Type, AnnouncementTypes, "Alert type");
        if (type == "Result" && !await db.GradeRecords.AnyAsync(cancellationToken) && !await db.AuditLogs.AnyAsync(item => item.Type == "Grade", cancellationToken))
            throw new InvalidOperationException("Result alerts require semester result data in Record History.");
        return type;
    }

    private static string Required(string? value, string label, int maximum)
    {
        var normalized = value?.Trim() ?? "";
        if (normalized.Length == 0) throw new ArgumentException($"{label} is required.");
        if (normalized.Length > maximum) throw new ArgumentException($"{label} must not exceed {maximum} characters.");
        return normalized;
    }

    private static string Choice(string? value, HashSet<string> allowed, string label)
    {
        var normalized = allowed.FirstOrDefault(item => item.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return normalized ?? throw new ArgumentException($"{label} is invalid.");
    }

    private static NotificationItemDto Notification(Notification item) => new(item.Id, item.NotificationCode, item.Type, item.Title, item.Message, item.Severity, item.IsRead, item.CreateAt);
    private static NotificationHistoryItemDto History(NotificationHistory item) => new(item.Id, item.NotificationHistoryCode, item.SourceId, item.SourceCode, item.Kind, item.Type, item.Title, item.Message, item.Action, item.CreateAt);
    private static Notification CreateNotification(string type, string title, string message) => new() { Type = type, Title = title, Message = message, Severity = Severity(type), IsRead = false };
    private static string Severity(string type) => type switch { "Emergency" => "Critical", "Attendance" => "Warning", _ => "Info" };
    private static AnnouncementItemDto Announcement(Announcement item) => new(item.Id, item.AnnouncementCode, item.NotificationId, item.Type, item.Title, item.Message, item.CreateAt);
}
