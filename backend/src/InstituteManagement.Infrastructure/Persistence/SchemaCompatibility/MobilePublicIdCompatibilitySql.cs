namespace InstituteManagement.Infrastructure.Persistence.SchemaCompatibility;

internal static class MobilePublicIdCompatibilitySql
{
    internal const string CommandText = """
        IF OBJECT_ID(N'[Students]', N'U') IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Students_PublicId' AND [object_id] = OBJECT_ID(N'[Students]') AND [filter_definition] IS NULL)
                DROP INDEX [IX_Students_PublicId] ON [Students];
            UPDATE [Students] SET [PublicId] = N'' WHERE [PublicId] <> N'';
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Students_PublicId' AND [object_id] = OBJECT_ID(N'[Students]'))
                CREATE UNIQUE INDEX [IX_Students_PublicId] ON [Students] ([PublicId]) WHERE [PublicId] <> N'';
        END;

        IF OBJECT_ID(N'[Teachers]', N'U') IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Teachers_PublicId' AND [object_id] = OBJECT_ID(N'[Teachers]') AND [filter_definition] IS NULL)
                DROP INDEX [IX_Teachers_PublicId] ON [Teachers];
            UPDATE [Teachers] SET [PublicId] = N'' WHERE [PublicId] <> N'';
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Teachers_PublicId' AND [object_id] = OBJECT_ID(N'[Teachers]'))
                CREATE UNIQUE INDEX [IX_Teachers_PublicId] ON [Teachers] ([PublicId]) WHERE [PublicId] <> N'';
        END;

        IF OBJECT_ID(N'[SystemSettings]', N'U') IS NOT NULL
            UPDATE [SystemSettings]
            SET [Value] = N'TEA', [UpdatedAtUtc] = SYSUTCDATETIME()
            WHERE [Section] = N'code-formats' AND [Key] = N'teacherPublicIdPrefix' AND [Value] <> N'TEA';

        IF OBJECT_ID(N'[Enrollment].[TeacherAssignments]', N'U') IS NOT NULL
            UPDATE [Enrollment].[TeacherAssignments]
            SET [PublicId] = N'TEA-' + UPPER(REPLACE(CONVERT(nvarchar(36), [Id]), N'-', N'')),
                [UpdatedAtUtc] = SYSUTCDATETIME()
            WHERE [PublicId] <> N'TEA-' + UPPER(REPLACE(CONVERT(nvarchar(36), [Id]), N'-', N''));
        """;
}
