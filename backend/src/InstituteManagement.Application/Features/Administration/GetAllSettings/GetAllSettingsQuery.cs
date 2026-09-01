using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Administration.GetAllSettings;

public sealed record GetAllSettingsQuery : IRequest<IReadOnlyList<SettingsDto>>;
