using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Notifications.Common;
using InstituteManagement.Application.Features.Notifications.Notifications;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Notifications.Notifications;

public sealed class NotificationService(InstituteDbContext db, InstituteCache cache) : INotificationService
{
    private static readonly string[] Severities = ["Info", "Warning", "Critical"];

    public async Task<PagedResult<NotificationItemDto>> GetSystemAsync(string? search, bool unreadOnly, bool prioritizeUnread, PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Notifications.AsNoTracking().Where(item => item.Type == "System");
        if (unreadOnly) query = query.Where(item => !item.IsRead);
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(item => item.NotificationCode.Contains(term) || item.Title.Contains(term) || item.Message.Contains(term) || item.Severity.Contains(term));
        var ordered = prioritizeUnread
            ? query.OrderBy(item => item.IsRead).ThenByDescending(item => item.CreateAt)
            : query.OrderByDescending(item => item.CreateAt);
        return await ordered
            .ThenByDescending(item => item.Id)
            .Select(item => new NotificationItemDto(
                item.Id,
                item.NotificationCode,
                item.Type,
                item.Title,
                item.Message,
                item.Severity,
                item.IsRead,
                item.CreateAt))
            .ToPagedResultAsync(page, cancellationToken);
    }

    public async Task<NotificationItemDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Notifications.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Notification not found.");
        return Map(entity);
    }

    public async Task<NotificationItemDto> MarkReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity.IsRead) return Map(entity);

        entity.IsRead = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await SaveAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<int> MarkAllReadAsync(CancellationToken cancellationToken)
    {
        var changedAt = DateTime.UtcNow;
        var changed = await db.Notifications
            .Where(item => item.Type == "System" && !item.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.UpdatedAtUtc, changedAt), cancellationToken);
        if (changed > 0) await cache.InvalidateDashboardAsync(cancellationToken);
        return changed;
    }

    public async Task<NotificationItemDto> UpdateAsync(Guid id, UpdateNotificationDto request, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        entity.Title = NotificationContentValidator.Required(request.Title, "Notification title", 200);
        entity.Message = NotificationContentValidator.Required(request.Message, "Notification detail", 2000);
        entity.Severity = NotificationContentValidator.Choice(request.Severity, Severities, "Notification severity");
        entity.IsRead = request.IsRead;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await SaveAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        var announcement = await db.Announcements.SingleOrDefaultAsync(item => item.NotificationId == id, cancellationToken);
        if (announcement is not null) announcement.NotificationId = null;
        db.Notifications.Remove(entity);
        await SaveAsync(cancellationToken);
    }

    private async Task<Notification> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Notifications.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException("Notification not found.");

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    private static NotificationItemDto Map(Notification item) =>
        new(item.Id, item.NotificationCode, item.Type, item.Title, item.Message, item.Severity, item.IsRead, item.CreateAt);
}
