using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Administration.GetSettings;

public sealed class GetSettingsHandler(ISettingsService service) : IRequestHandler<GetSettingsQuery, SettingsDto>
{
    public Task<SettingsDto> Handle(GetSettingsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Section, cancellationToken);
}
