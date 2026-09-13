using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Timetable;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Timetable;

public sealed class TimetableCatalogServiceTests
{
    [Fact]
    public async Task Create_allows_administrator_selected_shift_and_free_time_range()
    {
        await using var db = CreateContext();
        var service = new TimetableCatalogService(db, new InstituteCache());

        await service.CreateAsync(new Dictionary<string, string>
        {
            ["timetableCode"] = "TIM-731",
            ["dayOfWeek"] = "Monday",
            ["shift"] = "Morning",
            ["startsAt"] = "07:30",
            ["endsAt"] = "09:50",
            ["status"] = "Upcoming"
        }, CancellationToken.None);

        var schedule = Assert.Single(db.ScheduleEntries);
        Assert.Equal("Morning", schedule.Shift);
        Assert.Equal(new TimeOnly(7, 30), schedule.StartsAt);
        Assert.Equal(new TimeOnly(9, 50), schedule.EndsAt);
    }

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
