namespace InstituteManagement.Application.Features.Operations;

public sealed record OperationSummaryDto(string Module, string Summary, string Value, string Detail, string Status, string Route, string Tone);
