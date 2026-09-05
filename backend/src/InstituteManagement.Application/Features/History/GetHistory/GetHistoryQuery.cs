using InstituteManagement.Application.Features.Record;
using MediatR;

namespace InstituteManagement.Application.Features.History.GetHistory;

public sealed record GetHistoryQuery(string? Search, string? Type) : IRequest<IReadOnlyList<RecordDto>>;
