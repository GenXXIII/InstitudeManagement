using MediatR;

namespace InstituteManagement.Application.Features.Grades.UpdateGradeRecord;

public sealed class UpdateGradeRecordHandler(IGradeCatalogService service) : IRequestHandler<UpdateGradeRecordCommand, GradeResponseDto>
{
    public Task<GradeResponseDto> Handle(UpdateGradeRecordCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.Id, request.Values, cancellationToken);
}
