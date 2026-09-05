namespace InstituteManagement.Domain.Timetables;

public sealed record TeachingPeriod(string DayGroup, string Session, TimeOnly StartsAt, TimeOnly EndsAt);
