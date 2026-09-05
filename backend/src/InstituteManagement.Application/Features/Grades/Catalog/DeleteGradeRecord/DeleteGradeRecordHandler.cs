using MediatR;

namespace InstituteManagement.Application.Features.Grades.DeleteGradeRecord;

public sealed class DeleteGradeRecordHandler(IGradeCatalogService service) : IRequestHandler<DeleteGradeRecordCommand, bool>
{
    public Task<bool> Handle(DeleteGradeRecordCommand request, CancellationToken cancellationToken) =>
        service.DeleteAsync(request.Id, cancellationToken);
}
