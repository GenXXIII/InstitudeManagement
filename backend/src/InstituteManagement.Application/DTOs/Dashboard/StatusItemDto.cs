namespace InstituteManagement.Application.DTOs;

public sealed record StatusItemDto(string Label, string Value, string Detail, string Status = "Active");
