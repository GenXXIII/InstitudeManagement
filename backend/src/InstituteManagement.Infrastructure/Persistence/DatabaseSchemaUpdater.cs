using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class DatabaseSchemaUpdater
{
    public static async Task EnsureAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational()) return;
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('Students', 'PhotoDataUrl') IS NULL ALTER TABLE [Students] ADD [PhotoDataUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Students_PhotoDataUrl] DEFAULT '';
            IF COL_LENGTH('Teachers', 'PhotoDataUrl') IS NULL ALTER TABLE [Teachers] ADD [PhotoDataUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Teachers_PhotoDataUrl] DEFAULT '';
            IF COL_LENGTH('Departments', 'HeadTeacherId') IS NULL ALTER TABLE [Departments] ADD [HeadTeacherId] uniqueidentifier NULL;
            IF COL_LENGTH('Classrooms', 'DepartmentId') IS NULL ALTER TABLE [Classrooms] ADD [DepartmentId] uniqueidentifier NULL;
            IF COL_LENGTH('AuditLogs', 'ResourceId') IS NULL ALTER TABLE [AuditLogs] ADD [ResourceId] uniqueidentifier NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_HeadTeacherId') CREATE INDEX [IX_Departments_HeadTeacherId] ON [Departments] ([HeadTeacherId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Classrooms_DepartmentId') CREATE INDEX [IX_Classrooms_DepartmentId] ON [Classrooms] ([DepartmentId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_ResourceId') CREATE INDEX [IX_AuditLogs_ResourceId] ON [AuditLogs] ([ResourceId]);
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Departments_Teachers_HeadTeacherId') ALTER TABLE [Departments] ADD CONSTRAINT [FK_Departments_Teachers_HeadTeacherId] FOREIGN KEY ([HeadTeacherId]) REFERENCES [Teachers] ([Id]);
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Classrooms_Departments_DepartmentId') ALTER TABLE [Classrooms] ADD CONSTRAINT [FK_Classrooms_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]);
            """, cancellationToken);
    }
}
