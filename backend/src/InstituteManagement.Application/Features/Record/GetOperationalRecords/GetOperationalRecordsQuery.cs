using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Record.GetOperationalRecords;

public sealed record GetOperationalRecordsQuery(string Module, string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<OperationalRecordDto>>;
