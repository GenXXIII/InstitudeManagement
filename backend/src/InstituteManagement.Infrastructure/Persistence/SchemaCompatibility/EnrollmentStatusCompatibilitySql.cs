namespace InstituteManagement.Infrastructure.Persistence.SchemaCompatibility;

internal static class EnrollmentStatusCompatibilitySql
{
    internal const string CommandText = """
        UPDATE [Classrooms] SET [Status] = 'Available' WHERE [Status] = 'Running';
        UPDATE [Classrooms] SET [Status] = 'Maintenance' WHERE [Status] = 'Starting';
        UPDATE [Classrooms] SET [Status] = 'Maintenance' WHERE [Status] IN ('Offline', 'Unavailable');
        IF OBJECT_ID(N'[Enrollment].[ClassroomAssignments]', N'U') IS NOT NULL
            UPDATE [Enrollment].[ClassroomAssignments] SET [Status] = 'Maintenance' WHERE [Status] IN ('Reserved', 'Unavailable');
        """;
}
