using InstituteManagement.Application.DTOs.Notifications;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Notifications;

public sealed class NotificationCenterServiceTests
{
    [Fact]
    public async Task Alert_lifecycle_is_recorded_in_read_only_history()
    {
        await using var db = CreateContext();
        var service = new NotificationCenterService(db, new InstituteCache());

        var created = await service.CreateAnnouncementAsync(
            new AnnouncementRequestDto("Emergency", "Campus closure", "The campus is closed today."),
            CancellationToken.None);
        var published = Assert.Single(await service.GetNotificationsAsync(CancellationToken.None));
        var opened = await service.MarkNotificationReadAsync(published.Id, CancellationToken.None);
        var detail = await service.GetNotificationAsync(published.Id, CancellationToken.None);
        var updated = await service.UpdateAnnouncementAsync(
            created.Id,
            new AnnouncementRequestDto("General", "Campus reopened", "Normal operations have resumed."),
            CancellationToken.None);
        var refreshed = Assert.Single(await service.GetNotificationsAsync(CancellationToken.None));
        await service.DeleteAnnouncementAsync(created.Id, CancellationToken.None);

        Assert.StartsWith("ANN-", created.AnnouncementCode);
        Assert.Equal(created.NotificationId, published.Id);
        Assert.Equal("Emergency", published.Type);
        Assert.Equal("Critical", published.Severity);
        Assert.False(published.IsRead);
        Assert.True(opened.IsRead);
        Assert.True(detail.IsRead);
        Assert.Equal(updated.NotificationId, refreshed.Id);
        Assert.Equal("General", refreshed.Type);
        Assert.Equal("Info", refreshed.Severity);
        Assert.Equal("Campus reopened", refreshed.Title);
        Assert.Empty(await service.GetAnnouncementsAsync(CancellationToken.None));
        Assert.Empty(await service.GetNotificationsAsync(CancellationToken.None));
        var readHistory = Assert.Single(await service.GetHistoryAsync(CancellationToken.None));
        Assert.Equal("Read", readHistory.Action);
        Assert.Equal(readHistory, await service.GetHistoryItemAsync(readHistory.Id, CancellationToken.None));
        var history = await db.NotificationHistory.AsNoTracking().ToListAsync();
        Assert.Equal(["Announced", "Updated", "Removed"], history.Where(item => item.Kind == "Alert").OrderBy(item => item.CreateAt).Select(item => item.Action));
        Assert.Equal(["Created", "Read", "Updated", "Removed"], history.Where(item => item.Kind == "Notification").OrderBy(item => item.CreateAt).Select(item => item.Action));
        Assert.All(history, item => Assert.StartsWith("NHS-", item.NotificationHistoryCode));
    }

    [Fact]
    public async Task Result_alert_requires_semester_results_in_record_history()
    {
        await using var db = CreateContext();
        var service = new NotificationCenterService(db, new InstituteCache());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAnnouncementAsync(
            new AnnouncementRequestDto("Result", "Semester results", "Results are ready."),
            CancellationToken.None));

        Assert.Equal("Result alerts require semester result data in Record History.", error.Message);
        Assert.Empty(db.Announcements);
        Assert.Empty(db.NotificationHistory);
    }

    private static InstituteDbContext CreateContext() => new(
        new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
