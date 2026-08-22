using System.Data;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(InstituteDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational())
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        // Preserve upgrades for older schemas while leaving every data table empty on a fresh database.
        if (await db.Database.CanConnectAsync(cancellationToken) &&
            await HasExistingInstituteSchemaAsync(db, cancellationToken))
        {
            await DatabaseSchemaUpdater.EnsureAsync(db, cancellationToken);
            await MarkInitialMigrationAppliedAsync(db, cancellationToken);
        }

        await db.Database.MigrateAsync(cancellationToken);
        await DatabaseSchemaUpdater.EnsureAsync(db, cancellationToken);
    }

    private static async Task<bool> HasExistingInstituteSchemaAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        if (close) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CASE WHEN OBJECT_ID(N'[Departments]', N'U') IS NULL THEN 0 ELSE 1 END";
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        }
        finally
        {
            if (close) await connection.CloseAsync();
        }
    }

    private static Task MarkInitialMigrationAppliedAsync(InstituteDbContext db, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
            BEGIN
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END;
            IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260822023337_InitialInstituteSchema')
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260822023337_InitialInstituteSchema', N'10.0.11');
            """, cancellationToken);
}
