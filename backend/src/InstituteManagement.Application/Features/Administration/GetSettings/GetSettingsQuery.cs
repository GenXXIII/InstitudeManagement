using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Administration.GetSettings;

public sealed record GetSettingsQuery(string Section) : IRequest<SettingsDto>;
