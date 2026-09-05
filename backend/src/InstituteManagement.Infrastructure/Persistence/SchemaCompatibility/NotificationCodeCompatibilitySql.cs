namespace InstituteManagement.Infrastructure.Persistence.SchemaCompatibility;

internal static class NotificationCodeCompatibilitySql
{
    internal const string CommandText = """
        IF OBJECT_ID(N'[Notifications]', N'U') IS NOT NULL AND EXISTS (SELECT 1 FROM [Notifications] WHERE LEN([NotificationCode]) = 36 AND [NotificationCode] LIKE 'NOT-%' AND SUBSTRING([NotificationCode], 5, 32) NOT LIKE '%[^0-9A-Fa-f]%')
        BEGIN
            ;WITH [Numbered] AS (
                SELECT [Id], ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc], [Id]) AS [Sequence]
                FROM [Notifications]
            )
            UPDATE [item] SET [NotificationCode] = CONCAT('NOT-', RIGHT(CONCAT('00000000', [numbered].[Sequence]), 8))
            FROM [Notifications] [item] INNER JOIN [Numbered] [numbered] ON [numbered].[Id] = [item].[Id];
        END;

        IF OBJECT_ID(N'[Announcements]', N'U') IS NOT NULL AND EXISTS (SELECT 1 FROM [Announcements] WHERE LEN([AnnouncementCode]) = 36 AND [AnnouncementCode] LIKE 'ANN-%' AND SUBSTRING([AnnouncementCode], 5, 32) NOT LIKE '%[^0-9A-Fa-f]%')
        BEGIN
            ;WITH [Numbered] AS (
                SELECT [Id], ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc], [Id]) AS [Sequence]
                FROM [Announcements]
            )
            UPDATE [item] SET [AnnouncementCode] = CONCAT('ANN-', RIGHT(CONCAT('00000000', [numbered].[Sequence]), 8))
            FROM [Announcements] [item] INNER JOIN [Numbered] [numbered] ON [numbered].[Id] = [item].[Id];
        END;

        IF OBJECT_ID(N'[NotificationHistory]', N'U') IS NOT NULL
        BEGIN
            UPDATE [history]
            SET [SourceCode] = COALESCE([notification].[NotificationCode], [announcement].[AnnouncementCode], [history].[SourceCode])
            FROM [NotificationHistory] [history]
            LEFT JOIN [Notifications] [notification] ON [history].[Kind] = 'Notification' AND [notification].[Id] = [history].[SourceId]
            LEFT JOIN [Announcements] [announcement] ON [history].[Kind] = 'Alert' AND [announcement].[Id] = [history].[SourceId];

            IF EXISTS (SELECT 1 FROM [NotificationHistory] WHERE LEN([NotificationHistoryCode]) = 36 AND [NotificationHistoryCode] LIKE 'NHS-%' AND SUBSTRING([NotificationHistoryCode], 5, 32) NOT LIKE '%[^0-9A-Fa-f]%')
            BEGIN
                ;WITH [Numbered] AS (
                    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc], [Id]) AS [Sequence]
                    FROM [NotificationHistory]
                )
                UPDATE [item] SET [NotificationHistoryCode] = CONCAT('NHS-', RIGHT(CONCAT('00000000', [numbered].[Sequence]), 8))
                FROM [NotificationHistory] [item] INNER JOIN [Numbered] [numbered] ON [numbered].[Id] = [item].[Id];
            END;
        END;
        """;
}
