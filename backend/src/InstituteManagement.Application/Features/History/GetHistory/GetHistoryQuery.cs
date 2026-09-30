using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.History.GetHistory;

public sealed record GetHistoryQuery(string? Search, string? Type, PageRequest Page) : IRequest<HistoryPageDto>;
