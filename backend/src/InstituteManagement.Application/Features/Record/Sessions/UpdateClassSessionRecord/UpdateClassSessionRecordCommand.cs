using InstituteManagement.Application.Features.Record;
using MediatR;

namespace InstituteManagement.Application.Features.Record.UpdateClassSessionRecord;

public sealed record UpdateClassSessionRecordCommand(Guid Id, UpdateClassSessionRecordDto Update) : IRequest;
