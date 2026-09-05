using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public sealed partial class InstituteDbContext
{
    private void CaptureNotificationHistory()
    {
        var history = new List<NotificationHistory>();
        foreach (var entry in ChangeTracker.Entries<Notification>().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var item = entry.Entity;
            var action = entry.State == EntityState.Added ? "Created" : entry.State == EntityState.Deleted ? "Removed" : entry.Property(x => x.IsRead).IsModified && item.IsRead ? "Read" : "Updated";
            history.Add(new NotificationHistory { SourceId = item.Id, SourceCode = item.NotificationCode, Kind = "Notification", Type = item.Type, Title = item.Title, Message = item.Message, Action = action });
        }
        foreach (var entry in ChangeTracker.Entries<Announcement>().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var item = entry.Entity;
            var action = entry.State == EntityState.Added ? "Announced" : entry.State == EntityState.Deleted || !item.IsActive ? "Removed" : "Updated";
            history.Add(new NotificationHistory { SourceId = item.Id, SourceCode = item.AnnouncementCode, Kind = "Alert", Type = item.Type, Title = item.Title, Message = item.Message, Action = action });
        }
        if (history.Count > 0) NotificationHistory.AddRange(history);
    }
}
