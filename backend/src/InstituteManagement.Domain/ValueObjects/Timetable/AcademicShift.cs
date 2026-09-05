namespace InstituteManagement.Domain.Timetables;

public sealed record AcademicShift(
    string Name,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    IReadOnlyList<TeachingPeriod> Periods);
