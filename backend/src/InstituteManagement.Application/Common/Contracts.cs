namespace InstituteManagement.Application.Common;

public sealed record MetricDto(string Label, string Value, string Detail, string Tone = "blue");
public sealed record ActivityDto(string Time, string Title, string Detail, string Tone = "blue");
public sealed record ChartPointDto(string Label, decimal Value);
public sealed record StatusItemDto(string Label, string Value, string Detail, string Status = "Active");

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
    IReadOnlyList<ChartPointDto> GradeDistribution);

public sealed record OperationDto(
    string Module,
    string Title,
    string Description,
    IReadOnlyList<MetricDto> Metrics,
    IReadOnlyList<Dictionary<string, string>> Rows,
    IReadOnlyList<ActivityDto> Activity,
    IReadOnlyList<ActivityDto> Attention);

public sealed record RecordDto(Guid Id, DateTime Date, string Type, string Subject, string Action, string Details);
public sealed record CatalogItemDto(Guid Id, Dictionary<string, string> Values);
public sealed record SettingsDto(string Section, Dictionary<string, string> Values);

public interface IInstituteDataStore
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<OperationDto> GetOperationAsync(string module, Guid? departmentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecordDto>> GetRecordsAsync(string? search, string? type, CancellationToken cancellationToken);
    Task<IReadOnlyList<CatalogItemDto>> GetCatalogAsync(string resource, string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<CatalogItemDto> CreateCatalogAsync(string resource, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<CatalogItemDto> UpdateCatalogAsync(string resource, Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteCatalogAsync(string resource, Guid id, CancellationToken cancellationToken);
    Task<SettingsDto> GetSettingsAsync(string section, CancellationToken cancellationToken);
    Task<SettingsDto> SaveSettingsAsync(string section, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task RecordAttendanceAsync(Guid studentId, string status, CancellationToken cancellationToken);
    Task SubmitGradeAsync(Guid studentId, Guid courseId, decimal score, CancellationToken cancellationToken);
}

public interface ILiveUpdatePublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken);
}
