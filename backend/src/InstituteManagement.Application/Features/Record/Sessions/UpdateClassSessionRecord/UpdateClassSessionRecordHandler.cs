using InstituteManagement.Application.Features.Record;
using MediatR;

namespace InstituteManagement.Application.Features.Record.UpdateClassSessionRecord;

public sealed class UpdateClassSessionRecordHandler(IOperationalRecordEditService service) : IRequestHandler<UpdateClassSessionRecordCommand>
{
    public Task Handle(UpdateClassSessionRecordCommand request, CancellationToken cancellationToken) =>
        service.UpdateClassSessionAsync(request.Id, request.Update, cancellationToken);
}
