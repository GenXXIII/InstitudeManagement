namespace InstituteManagement.Application.DTOs;

public sealed record OperationSummaryDto(string Module, string Summary, string Value, string Detail, string Status, string Route, string Tone);
