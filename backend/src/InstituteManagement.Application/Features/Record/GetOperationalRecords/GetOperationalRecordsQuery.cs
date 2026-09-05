using InstituteManagement.Application.Features.Record;
using MediatR;

namespace InstituteManagement.Application.Features.Record.GetOperationalRecords;

public sealed record GetOperationalRecordsQuery(string Module, string? Search, Guid? DepartmentId, bool History) : IRequest<IReadOnlyList<OperationalRecordDto>>;
