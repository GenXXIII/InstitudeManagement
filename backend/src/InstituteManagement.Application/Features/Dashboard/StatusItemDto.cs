namespace InstituteManagement.Application.Features.Dashboard;

public sealed record StatusItemDto(string Label, string Value, string Detail, string Status = "Active");
