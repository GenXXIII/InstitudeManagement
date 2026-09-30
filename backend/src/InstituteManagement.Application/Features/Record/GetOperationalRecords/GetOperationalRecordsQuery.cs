using InstituteManagement.Application.Features.Record;
using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Record.GetOperationalRecords;

public sealed record GetOperationalRecordsQuery(string Module, string? Search, Guid? DepartmentId, int? Year, string? Period, bool History, PageRequest Page) : IRequest<PagedResult<OperationalRecordDto>>;
