using InstituteManagement.Application.Features.Administration;
using MediatR;

namespace InstituteManagement.Application.Features.Administration.SaveSettings;

public sealed record SaveSettingsCommand(string Section, Dictionary<string, string> Values) : IRequest<SettingsDto>;
