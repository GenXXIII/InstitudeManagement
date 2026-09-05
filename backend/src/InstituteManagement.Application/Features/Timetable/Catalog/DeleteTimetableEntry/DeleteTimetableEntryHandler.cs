using MediatR;

namespace InstituteManagement.Application.Features.Timetable.DeleteTimetableEntry;

public sealed class DeleteTimetableEntryHandler(ITimetableCatalogService service) : IRequestHandler<DeleteTimetableEntryCommand, bool>
{
    public Task<bool> Handle(DeleteTimetableEntryCommand request, CancellationToken cancellationToken) =>
        service.DeleteAsync(request.Id, cancellationToken);
}
