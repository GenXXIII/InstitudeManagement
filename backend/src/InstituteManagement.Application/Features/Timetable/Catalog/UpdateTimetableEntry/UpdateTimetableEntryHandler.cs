using MediatR;

namespace InstituteManagement.Application.Features.Timetable.UpdateTimetableEntry;

public sealed class UpdateTimetableEntryHandler(ITimetableCatalogService service) : IRequestHandler<UpdateTimetableEntryCommand, TimetableResponseDto>
{
    public Task<TimetableResponseDto> Handle(UpdateTimetableEntryCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.Id, request.Values, cancellationToken);
}
