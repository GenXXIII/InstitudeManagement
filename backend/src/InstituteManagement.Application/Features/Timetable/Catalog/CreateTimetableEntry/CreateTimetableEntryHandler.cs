using MediatR;

namespace InstituteManagement.Application.Features.Timetable.CreateTimetableEntry;

public sealed class CreateTimetableEntryHandler(ITimetableCatalogService service) : IRequestHandler<CreateTimetableEntryCommand, TimetableResponseDto>
{
    public Task<TimetableResponseDto> Handle(CreateTimetableEntryCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Values, cancellationToken);
}
