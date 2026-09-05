using InstituteManagement.Application.Features.Administration;
using MediatR;

namespace InstituteManagement.Application.Features.Administration.SaveSettings;

public sealed class SaveSettingsHandler(ISettingsService service) : IRequestHandler<SaveSettingsCommand, SettingsDto>
{
    public Task<SettingsDto> Handle(SaveSettingsCommand request, CancellationToken cancellationToken) =>
        service.SaveAsync(request.Section, request.Values, cancellationToken);
}
