namespace InstituteManagement.Application.DTOs;

public sealed record DashboardDto(
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
