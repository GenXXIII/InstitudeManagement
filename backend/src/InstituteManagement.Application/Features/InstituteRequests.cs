using InstituteManagement.Application.Common;
using MediatR;

namespace InstituteManagement.Application.Features;

public sealed record GetDashboardQuery : IRequest<DashboardDto>;
public sealed record GetOperationQuery(string Module, Guid? DepartmentId) : IRequest<OperationDto>;
public sealed record GetRecordsQuery(string? Search, string? Type) : IRequest<IReadOnlyList<RecordDto>>;
public sealed record GetCatalogQuery(string Resource, string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<CatalogItemDto>>;
public sealed record CreateCatalogCommand(string Resource, Dictionary<string, string> Values) : IRequest<CatalogItemDto>;
public sealed record UpdateCatalogCommand(string Resource, Guid Id, Dictionary<string, string> Values) : IRequest<CatalogItemDto>;
public sealed record DeleteCatalogCommand(string Resource, Guid Id) : IRequest<bool>;
public sealed record GetSettingsQuery(string Section) : IRequest<SettingsDto>;
public sealed record SaveSettingsCommand(string Section, Dictionary<string, string> Values) : IRequest<SettingsDto>;
public sealed record RecordAttendanceCommand(Guid StudentId, string Status) : IRequest;
public sealed record SubmitGradeCommand(Guid StudentId, Guid CourseId, decimal Score) : IRequest;

public sealed class InstituteRequestHandlers(IInstituteDataStore store, ILiveUpdatePublisher publisher) :
    IRequestHandler<GetDashboardQuery, DashboardDto>,
    IRequestHandler<GetOperationQuery, OperationDto>,
    IRequestHandler<GetRecordsQuery, IReadOnlyList<RecordDto>>,
    IRequestHandler<GetCatalogQuery, IReadOnlyList<CatalogItemDto>>,
    IRequestHandler<CreateCatalogCommand, CatalogItemDto>,
    IRequestHandler<UpdateCatalogCommand, CatalogItemDto>,
    IRequestHandler<DeleteCatalogCommand, bool>,
    IRequestHandler<GetSettingsQuery, SettingsDto>,
    IRequestHandler<SaveSettingsCommand, SettingsDto>,
    IRequestHandler<RecordAttendanceCommand>,
    IRequestHandler<SubmitGradeCommand>
{
    public Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct) => store.GetDashboardAsync(ct);
    public Task<OperationDto> Handle(GetOperationQuery request, CancellationToken ct) => store.GetOperationAsync(request.Module, request.DepartmentId, ct);
    public Task<IReadOnlyList<RecordDto>> Handle(GetRecordsQuery request, CancellationToken ct) => store.GetRecordsAsync(request.Search, request.Type, ct);
    public Task<IReadOnlyList<CatalogItemDto>> Handle(GetCatalogQuery request, CancellationToken ct) => store.GetCatalogAsync(request.Resource, request.Search, request.DepartmentId, ct);
    public Task<CatalogItemDto> Handle(CreateCatalogCommand request, CancellationToken ct) => store.CreateCatalogAsync(request.Resource, request.Values, ct);
    public Task<CatalogItemDto> Handle(UpdateCatalogCommand request, CancellationToken ct) => store.UpdateCatalogAsync(request.Resource, request.Id, request.Values, ct);
    public Task<bool> Handle(DeleteCatalogCommand request, CancellationToken ct) => store.DeleteCatalogAsync(request.Resource, request.Id, ct);
    public Task<SettingsDto> Handle(GetSettingsQuery request, CancellationToken ct) => store.GetSettingsAsync(request.Section, ct);
    public Task<SettingsDto> Handle(SaveSettingsCommand request, CancellationToken ct) => store.SaveSettingsAsync(request.Section, request.Values, ct);

    public async Task Handle(RecordAttendanceCommand request, CancellationToken ct)
    {
        await store.RecordAttendanceAsync(request.StudentId, request.Status, ct);
        await publisher.PublishAsync("ATTENDANCE_RECORDED", new { request.StudentId, request.Status, RecordedAt = DateTime.UtcNow }, ct);
    }

    public async Task Handle(SubmitGradeCommand request, CancellationToken ct)
    {
        await store.SubmitGradeAsync(request.StudentId, request.CourseId, request.Score, ct);
        await publisher.PublishAsync("GRADE_SUBMITTED", new { request.StudentId, request.CourseId, request.Score }, ct);
    }
}
