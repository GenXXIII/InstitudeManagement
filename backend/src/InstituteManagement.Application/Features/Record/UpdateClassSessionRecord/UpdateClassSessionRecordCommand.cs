using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Record.UpdateClassSessionRecord;

public sealed record UpdateClassSessionRecordCommand(Guid Id, UpdateClassSessionRecordDto Update) : IRequest;
