using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed record OperationContext(string Scope, IReadOnlyList<ActivityDto> Activity, IReadOnlyList<ActivityDto> Attention);
