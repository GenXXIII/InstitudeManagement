namespace InstituteManagement.Application.DTOs;

public sealed record MetricDto(string Label, string Value, string Detail, string Tone = "blue");
