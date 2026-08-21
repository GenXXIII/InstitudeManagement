namespace InstituteManagement.Application.DTOs;

public sealed record CatalogItemDto(Guid Id, Dictionary<string, string> Values);
