namespace InstituteManagement.Application.Features.Dashboard;

public sealed record MetricDto(string Label, string Value, string Detail, string Tone = "blue");
