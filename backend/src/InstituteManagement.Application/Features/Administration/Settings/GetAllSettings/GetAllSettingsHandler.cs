using InstituteManagement.Application.Features.Administration;
using MediatR;

namespace InstituteManagement.Application.Features.Administration.GetAllSettings;

public sealed class GetAllSettingsHandler(ISettingsService service) : IRequestHandler<GetAllSettingsQuery, IReadOnlyList<SettingsDto>>
{
    public Task<IReadOnlyList<SettingsDto>> Handle(GetAllSettingsQuery request, CancellationToken cancellationToken) =>
        service.GetAllAsync(cancellationToken);
}
