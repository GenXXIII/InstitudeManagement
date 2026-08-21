using InstituteManagement.Application.Features.Timetable.GetTeachingPeriods;

namespace InstituteManagement.Application.Tests.Timetable;

public sealed class GetTeachingPeriodsHandlerTests
{
    [Fact]
    public async Task Returns_backend_owned_weekday_and_weekend_periods()
    {
        var periods = await new GetTeachingPeriodsHandler().Handle(new GetTeachingPeriodsQuery(), CancellationToken.None);

        Assert.Equal(12, periods.Count);
        Assert.Equal(7, periods.Count(period => period.DayGroup == "Weekday"));
        Assert.Equal(5, periods.Count(period => period.DayGroup == "Weekend"));
        Assert.DoesNotContain(periods, period => period.DayGroup == "Weekend" && period.Session == "Evening");
    }
}
