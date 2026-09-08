namespace InstituteManagement.Application.Common.Exceptions;

public sealed class PersistenceConflictException(Exception innerException)
    : Exception(
        "This record duplicates an existing ID or unique relationship. Change the ID or selected relationship and try again.",
        innerException);
