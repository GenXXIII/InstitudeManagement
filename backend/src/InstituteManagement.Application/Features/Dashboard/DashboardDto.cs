namespace InstituteManagement.Application.Features.Dashboard;

public sealed record DashboardDto(
    string Range,
    string RangeLabel,
    string PeriodStart,
    string PeriodEnd,
    DateTime GeneratedAt,
    IReadOnlyList<MetricDto> Metrics,
    decimal AttendanceRate,
    decimal AttendanceChange,
    IReadOnlyList<StatusItemDto> LiveStatus,
    IReadOnlyList<StatusItemDto> TodaySchedule,
    IReadOnlyList<ChartPointDto> AttendanceTrend,
    IReadOnlyList<ActivityDto> Attention,
    IReadOnlyList<ActivityDto> Activity,
    IReadOnlyList<StatusItemDto> DepartmentStatus,
    decimal AverageGrade,
    IReadOnlyList<ChartPointDto> GradeDistribution);
