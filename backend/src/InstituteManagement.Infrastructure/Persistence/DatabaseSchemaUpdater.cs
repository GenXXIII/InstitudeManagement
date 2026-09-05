using InstituteManagement.Infrastructure.Persistence.SchemaCompatibility;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class DatabaseSchemaUpdater
{
    public static async Task EnsureAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational()) return;

        var commandText = string.Join(
            Environment.NewLine,
            CoreSchemaCompatibilitySql.CommandText,
            EnrollmentStatusCompatibilitySql.CommandText,
            NotificationCodeCompatibilitySql.CommandText,
            TimetablePeriodCompatibilitySql.CommandText);

        await db.Database.ExecuteSqlRawAsync(commandText, cancellationToken);
    }
}
