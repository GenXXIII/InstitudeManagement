using InstituteManagement.Application.Features.Notifications.Announcements;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Notifications.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Notifications.Announcements;

public sealed class AnnouncementService(
    InstituteDbContext db,
    InstituteCache cache,
    AnnouncementPolicy policy) : IAnnouncementService
{
    public async Task<IReadOnlyList<AnnouncementItemDto>> GetAsync(CancellationToken cancellationToken) =>
        await db.Announcements.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.CreateAt)
            .Select(item => new AnnouncementItemDto(
                item.Id,
                item.AnnouncementCode,
                item.NotificationId,
                item.Type,
                item.Title,
                item.Message,
                item.CreateAt))
            .ToListAsync(cancellationToken);

    public async Task<AnnouncementItemDto> CreateAsync(AnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        var code = NotificationContentValidator.Code(request.AnnouncementCode, "AnnouncementCode");
        if (await db.Announcements.AnyAsync(item => item.AnnouncementCode == code, cancellationToken))
            throw new InvalidOperationException("AnnouncementCode already exists.");
        var type = await policy.ValidateTypeAsync(request.Type, cancellationToken);
        var title = NotificationContentValidator.Required(request.Title, "Alert title", 200);
        var message = NotificationContentValidator.Required(request.Message, "Alert detail", 2000);
        var entity = new Announcement
        {
            AnnouncementCode = code,
            Type = type,
            Title = title,
            Message = message,
            Notification = CreateNotification(type, title, message)
        };
        db.Announcements.Add(entity);
        await SaveAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<AnnouncementItemDto> UpdateAsync(Guid id, AnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        var code = NotificationContentValidator.Code(request.AnnouncementCode, "AnnouncementCode");
        if (await db.Announcements.AnyAsync(item => item.Id != id && item.AnnouncementCode == code, cancellationToken))
            throw new InvalidOperationException("AnnouncementCode already exists.");
        entity.AnnouncementCode = code;
        entity.Type = await policy.ValidateTypeAsync(request.Type, cancellationToken);
        entity.Title = NotificationContentValidator.Required(request.Title, "Alert title", 200);
        entity.Message = NotificationContentValidator.Required(request.Message, "Alert detail", 2000);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        entity.Notification ??= CreateNotification(entity.Type, entity.Title, entity.Message);
        UpdateNotification(entity.Notification, entity);
        await SaveAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        var notification = entity.Notification;
        db.Announcements.Remove(entity);
        if (notification is not null) db.Notifications.Remove(notification);
        await SaveAsync(cancellationToken);
    }

    private async Task<Announcement> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Announcements.Include(item => item.Notification)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Alert not found.");

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    private static Notification CreateNotification(string type, string title, string message) => new()
    {
        Type = type,
        Title = title,
        Message = message,
        Severity = NotificationContentValidator.SeverityFor(type),
        IsRead = false
    };

    private static void UpdateNotification(Notification notification, Announcement announcement)
    {
        notification.Title = announcement.Title;
        notification.Message = announcement.Message;
        notification.Type = announcement.Type;
        notification.Severity = NotificationContentValidator.SeverityFor(announcement.Type);
        notification.IsRead = false;
        notification.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static AnnouncementItemDto Map(Announcement item) =>
        new(item.Id, item.AnnouncementCode, item.NotificationId, item.Type, item.Title, item.Message, item.CreateAt);
}
