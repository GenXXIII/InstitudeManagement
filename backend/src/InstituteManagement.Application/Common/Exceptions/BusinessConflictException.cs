namespace InstituteManagement.Application.Common.Exceptions;

public sealed class BusinessConflictException(string title, string detail) : Exception(detail)
{
    public string Title { get; } = title;
}
