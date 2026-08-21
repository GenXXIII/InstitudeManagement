using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Services.Record;

public static class OperationalRecordFields
{
    public static Dictionary<string, string> Create(params (string Key, string Value)[] values) => values.ToDictionary(x => x.Key, x => x.Value);
    public static DateTime AttendanceDate(AttendanceRecord item) => item.Date.ToDateTime(item.CheckedInAt ?? TimeOnly.MinValue, DateTimeKind.Utc);
}
