namespace InstituteManagement.Domain.Timetables;

public static class AcademicTimetablePolicy
{
    private static readonly IReadOnlyList<TeachingPeriod> WeekdayPeriods =
    [
        Period("Weekday", "Morning", 7, 30, 9, 0),
        Period("Weekday", "Morning", 9, 15, 10, 45),
        Period("Weekday", "Morning", 11, 0, 12, 30),
        Period("Weekday", "Afternoon", 14, 0, 15, 30),
        Period("Weekday", "Afternoon", 15, 30, 17, 0),
        Period("Weekday", "Evening", 17, 30, 19, 0),
        Period("Weekday", "Evening", 19, 0, 20, 30)
    ];

    private static readonly IReadOnlyList<TeachingPeriod> WeekendPeriods =
    [
        Period("Weekend", "Morning", 7, 0, 8, 30),
        Period("Weekend", "Morning", 8, 40, 10, 10),
        Period("Weekend", "Morning", 11, 40, 13, 10),
        Period("Weekend", "Afternoon", 14, 0, 15, 30),
        Period("Weekend", "Afternoon", 15, 40, 17, 10)
    ];

    public static IReadOnlyList<TeachingPeriod> All => [.. WeekdayPeriods, .. WeekendPeriods];

    public static IReadOnlyList<TeachingPeriod> ForDay(DayOfWeek day) => day is DayOfWeek.Saturday or DayOfWeek.Sunday
        ? WeekendPeriods
        : WeekdayPeriods;

    public static TeachingPeriod? Find(DayOfWeek day, TimeOnly startsAt, TimeOnly endsAt) =>
        ForDay(day).FirstOrDefault(period => period.StartsAt == startsAt && period.EndsAt == endsAt);

    private static TeachingPeriod Period(string dayGroup, string session, int startHour, int startMinute, int endHour, int endMinute) =>
        new(dayGroup, session, new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));
}
