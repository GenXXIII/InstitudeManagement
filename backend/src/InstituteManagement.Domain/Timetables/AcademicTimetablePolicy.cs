namespace InstituteManagement.Domain.Timetables;

public static class AcademicTimetablePolicy
{
    public const string DefaultShiftName = "Morning";

    private static readonly IReadOnlyList<AcademicShift> AcademicShifts =
    [
        WeekdayShift("Morning", 7, 30, 10, 30),
        WeekdayShift("Afternoon", 14, 0, 17, 0),
        WeekdayShift("Evening", 17, 30, 20, 30),
        WeekendShift()
    ];

    private static readonly IReadOnlyList<TeachingPeriod> TeachingPeriods =
        AcademicShifts.SelectMany(shift => shift.Periods).ToList();

    public static IReadOnlyList<AcademicShift> Shifts => AcademicShifts;
    public static IReadOnlyList<string> ShiftNames => AcademicShifts.Select(shift => shift.Name).ToList();
    public static AcademicShift DefaultShift => AcademicShifts[0];
    public static IReadOnlyList<TeachingPeriod> All => TeachingPeriods;

    public static IReadOnlyList<TeachingPeriod> ForDay(DayOfWeek day)
    {
        var dayGroup = IsWeekend(day) ? "Weekend" : "Weekday";
        return TeachingPeriods.Where(period => period.DayGroup == dayGroup).ToList();
    }

    public static TeachingPeriod? Find(DayOfWeek day, TimeOnly startsAt, TimeOnly endsAt) =>
        ForDay(day).FirstOrDefault(period => period.StartsAt == startsAt && period.EndsAt == endsAt);

    public static AcademicShift? FindShift(string name) =>
        AcademicShifts.FirstOrDefault(shift => shift.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static AcademicShift? FindShift(DayOfWeek day, TimeOnly startsAt, TimeOnly endsAt)
    {
        var period = Find(day, startsAt, endsAt);
        return period is null ? null : AcademicShifts.FirstOrDefault(shift => shift.Periods.Contains(period));
    }

    public static AcademicPeriodSelection SelectCurrentOrNext(DateTime localNow)
    {
        var date = DateOnly.FromDateTime(localNow);
        var time = TimeOnly.FromDateTime(localNow);
        var periodsToday = ForDay(localNow.DayOfWeek);
        var current = periodsToday.FirstOrDefault(period => period.StartsAt <= time && period.EndsAt > time);
        if (current is not null) return Selection(current, date, true);

        var nextToday = periodsToday.FirstOrDefault(period => period.StartsAt > time);
        if (nextToday is not null) return Selection(nextToday, date, false);

        for (var offset = 1; offset <= 7; offset++)
        {
            var candidateDate = date.AddDays(offset);
            var nextPeriod = ForDay(candidateDate.DayOfWeek).FirstOrDefault();
            if (nextPeriod is not null) return Selection(nextPeriod, candidateDate, false);
        }

        throw new InvalidOperationException("No institute teaching period is configured.");
    }

    private static AcademicPeriodSelection Selection(TeachingPeriod period, DateOnly date, bool isRunning)
    {
        var shift = AcademicShifts.First(item => item.Periods.Contains(period));
        return new AcademicPeriodSelection(shift, period, date, isRunning);
    }

    private static bool IsWeekend(DayOfWeek day) => day is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static AcademicShift WeekdayShift(string name, int startHour, int startMinute, int endHour, int endMinute)
    {
        var startsAt = new TimeOnly(startHour, startMinute);
        var midpoint = startsAt.AddMinutes(90);
        var endsAt = new TimeOnly(endHour, endMinute);
        return new AcademicShift(name, startsAt, endsAt,
        [
            new TeachingPeriod("Weekday", name, startsAt, midpoint),
            new TeachingPeriod("Weekday", name, midpoint, endsAt)
        ]);
    }

    private static AcademicShift WeekendShift()
    {
        IReadOnlyList<TeachingPeriod> periods =
        [
            Period("Weekend", "Morning", 7, 0, 8, 30),
            Period("Weekend", "Morning", 8, 40, 10, 10),
            Period("Weekend", "Morning", 11, 40, 13, 10),
            Period("Weekend", "Afternoon", 14, 0, 15, 30),
            Period("Weekend", "Afternoon", 15, 40, 17, 10)
        ];
        return new AcademicShift("Weekend", periods[0].StartsAt, periods[^1].EndsAt, periods);
    }

    private static TeachingPeriod Period(string dayGroup, string session, int startHour, int startMinute, int endHour, int endMinute) =>
        new(dayGroup, session, new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));
}

public sealed record AcademicPeriodSelection(
    AcademicShift Shift,
    TeachingPeriod Period,
    DateOnly Date,
    bool IsRunning);
