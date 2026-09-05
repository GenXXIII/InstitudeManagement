namespace InstituteManagement.Infrastructure.Persistence.SchemaCompatibility;

internal static class TimetablePeriodCompatibilitySql
{
    internal const string CommandText = """
        UPDATE [ScheduleEntries]
        SET [StartsAt] = CASE
                WHEN [StartsAt] = CAST('08:00' AS time) AND [EndsAt] = CAST('09:00' AS time) THEN CAST('07:30' AS time)
                WHEN [StartsAt] = CAST('09:00' AS time) AND [EndsAt] = CAST('10:00' AS time) THEN CAST('09:15' AS time)
                WHEN [StartsAt] = CAST('10:00' AS time) AND [EndsAt] = CAST('11:00' AS time) THEN CAST('11:00' AS time)
                WHEN [StartsAt] = CAST('11:00' AS time) AND [EndsAt] = CAST('12:00' AS time) THEN CAST('14:00' AS time)
                WHEN [StartsAt] = CAST('12:00' AS time) AND [EndsAt] = CAST('13:00' AS time) THEN CAST('15:30' AS time)
                WHEN [StartsAt] = CAST('13:00' AS time) AND [EndsAt] = CAST('14:00' AS time) THEN CAST('17:30' AS time)
                ELSE [StartsAt]
            END,
            [EndsAt] = CASE
                WHEN [StartsAt] = CAST('08:00' AS time) AND [EndsAt] = CAST('09:00' AS time) THEN CAST('09:00' AS time)
                WHEN [StartsAt] = CAST('09:00' AS time) AND [EndsAt] = CAST('10:00' AS time) THEN CAST('10:45' AS time)
                WHEN [StartsAt] = CAST('10:00' AS time) AND [EndsAt] = CAST('11:00' AS time) THEN CAST('12:30' AS time)
                WHEN [StartsAt] = CAST('11:00' AS time) AND [EndsAt] = CAST('12:00' AS time) THEN CAST('15:30' AS time)
                WHEN [StartsAt] = CAST('12:00' AS time) AND [EndsAt] = CAST('13:00' AS time) THEN CAST('17:00' AS time)
                WHEN [StartsAt] = CAST('13:00' AS time) AND [EndsAt] = CAST('14:00' AS time) THEN CAST('19:00' AS time)
                ELSE [EndsAt]
            END
        WHERE [DayOfWeek] BETWEEN 1 AND 5
          AND (([StartsAt] = CAST('08:00' AS time) AND [EndsAt] = CAST('09:00' AS time))
            OR ([StartsAt] = CAST('09:00' AS time) AND [EndsAt] = CAST('10:00' AS time))
            OR ([StartsAt] = CAST('10:00' AS time) AND [EndsAt] = CAST('11:00' AS time))
            OR ([StartsAt] = CAST('11:00' AS time) AND [EndsAt] = CAST('12:00' AS time))
            OR ([StartsAt] = CAST('12:00' AS time) AND [EndsAt] = CAST('13:00' AS time))
            OR ([StartsAt] = CAST('13:00' AS time) AND [EndsAt] = CAST('14:00' AS time)));
        """;
}
