using InstituteManagement.Domain.Timetables;

namespace InstituteManagement.Infrastructure.Tests.Timetable;

public sealed class AcademicTimetablePolicyTests
{
    [Fact]
    public void Weekdays_have_morning_afternoon_and_evening_periods()
    {
        var periods = AcademicTimetablePolicy.ForDay(DayOfWeek.Monday);

        Assert.Equal(7, periods.Count);
        Assert.Equal(new TimeOnly(7, 30), periods[0].StartsAt);
        Assert.Equal(new TimeOnly(20, 30), periods[^1].EndsAt);
        Assert.Contains(periods, period => period.Session == "Evening");
    }

    [Fact]
    public void Weekends_have_only_morning_and_afternoon_periods()
    {
        var periods = AcademicTimetablePolicy.ForDay(DayOfWeek.Saturday);

        Assert.Equal(5, periods.Count);
        Assert.Equal(new TimeOnly(7, 0), periods[0].StartsAt);
        Assert.Equal(new TimeOnly(17, 10), periods[^1].EndsAt);
        Assert.DoesNotContain(periods, period => period.Session == "Evening");
    }
}
