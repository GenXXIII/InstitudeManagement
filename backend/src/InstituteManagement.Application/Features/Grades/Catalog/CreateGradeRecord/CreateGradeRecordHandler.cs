using MediatR;

namespace InstituteManagement.Application.Features.Grades.CreateGradeRecord;

public sealed class CreateGradeRecordHandler(IGradeCatalogService service) : IRequestHandler<CreateGradeRecordCommand, GradeResponseDto>
{
    public Task<GradeResponseDto> Handle(CreateGradeRecordCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Values, cancellationToken);
}
