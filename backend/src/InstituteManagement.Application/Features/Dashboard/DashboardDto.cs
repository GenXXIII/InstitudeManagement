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
    IReadOnlyList<ChartPointDto> AttendanceTrend,
    IReadOnlyList<StatusItemDto> DepartmentStatus,
    decimal AverageGrade,
    IReadOnlyList<ChartPointDto> GradeDistribution,
    FinanceSummaryDto Finance);

public sealed record FinanceSummaryDto(
    string Currency,
    string PeriodLabel,
    decimal TotalDue,
    decimal Collected,
    decimal Outstanding,
    decimal CollectionRate,
    int PaidAccounts,
    int OpenAccounts);
