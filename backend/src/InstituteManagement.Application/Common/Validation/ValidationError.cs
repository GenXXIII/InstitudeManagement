namespace InstituteManagement.Application.Common.Validation;

public sealed record ValidationError(string PropertyName, string Message);
