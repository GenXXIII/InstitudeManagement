using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Notifications.Common;
using InstituteManagement.Application.Features.Notifications.History;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Notifications.History;

public sealed class NotificationHistoryService(InstituteDbContext db) : INotificationHistoryService
{
    public async Task<PagedResult<NotificationHistoryItemDto>> GetAsync(string? search, PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.NotificationHistory.AsNoTracking()
            .Where(item => item.Kind == "Alert" || item.Kind == "Notification" && item.Type == "System");
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(item => item.NotificationHistoryCode.Contains(term) || item.SourceCode.Contains(term) || item.Title.Contains(term) || item.Message.Contains(term) || item.Action.Contains(term));
        return await query
            .OrderByDescending(item => item.CreateAt)
            .ThenByDescending(item => item.Id)
            .Select(item => new NotificationHistoryItemDto(
                item.Id,
                item.NotificationHistoryCode,
                item.SourceId,
                item.SourceCode,
                item.Kind,
                item.Type,
                item.Title,
                item.Message,
                item.Action,
                item.CreateAt))
            .ToPagedResultAsync(page, cancellationToken);
    }

    public async Task<NotificationHistoryItemDto> GetAsync(string codeOrId, CancellationToken cancellationToken)
    {
        var key = NotificationContentValidator.Required(codeOrId, "Notification history code", 64);
        var item = Guid.TryParse(key, out var id)
            ? await db.NotificationHistory.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id || item.NotificationHistoryCode == key, cancellationToken)
            : await db.NotificationHistory.AsNoTracking().SingleOrDefaultAsync(item => item.NotificationHistoryCode == key, cancellationToken);
        return item is null ? throw new KeyNotFoundException("Notification history not found.") : Map(item);
    }

    private static NotificationHistoryItemDto Map(NotificationHistory item) =>
        new(item.Id, item.NotificationHistoryCode, item.SourceId, item.SourceCode, item.Kind, item.Type, item.Title, item.Message, item.Action, item.CreateAt);
}
