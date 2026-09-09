using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeOrphanAlertHistoryCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @AlertPrefix nvarchar(16) = COALESCE(
                    (SELECT TOP (1) NULLIF(LTRIM(RTRIM([Value])), '')
                     FROM [SystemSettings]
                     WHERE [Section] = 'code-formats' AND [Key] = 'alertCodePrefix'),
                    N'ALT');
                DECLARE @Separator nvarchar(1) = COALESCE(
                    (SELECT TOP (1) CASE WHEN [Value] IN ('-', '/', '.', '_') THEN [Value] END
                     FROM [SystemSettings]
                     WHERE [Section] = 'code-formats' AND [Key] = 'codeSeparator'),
                    N'-');
                DECLARE @IncludeYear bit = CASE WHEN EXISTS (
                    SELECT 1 FROM [SystemSettings]
                    WHERE [Section] = 'code-formats' AND [Key] = 'codeIncludeYear' AND LOWER([Value]) = 'true')
                    THEN 1 ELSE 0 END;
                DECLARE @Year nvarchar(4) = CONVERT(nvarchar(4), YEAR(GETUTCDATE()));
                DECLARE @Padding int = COALESCE(
                    (SELECT TOP (1) TRY_CONVERT(int, [Value])
                     FROM [SystemSettings]
                     WHERE [Section] = 'code-formats' AND [Key] = 'codePaddingWidth'),
                    1);
                DECLARE @StartingNumber bigint = COALESCE(
                    (SELECT TOP (1) TRY_CONVERT(bigint, [Value])
                     FROM [SystemSettings]
                     WHERE [Section] = 'code-formats' AND [Key] = 'codeStartingNumber'),
                    1);
                SET @Padding = CASE WHEN @Padding < 1 THEN 1 WHEN @Padding > 12 THEN 12 ELSE @Padding END;
                SET @StartingNumber = CASE WHEN @StartingNumber < 0 THEN 1 ELSE @StartingNumber END;

                DECLARE @Stem nvarchar(32) = @AlertPrefix + @Separator
                    + CASE WHEN @IncludeYear = 1 THEN @Year + @Separator ELSE N'' END;
                DECLARE @MaxSequence bigint = COALESCE((
                    SELECT MAX([codes].[Sequence])
                    FROM
                    (
                        SELECT TRY_CONVERT(bigint, SUBSTRING([AnnouncementCode], LEN(@Stem) + 1, 64)) AS [Sequence]
                        FROM [Announcements]
                        WHERE [AnnouncementCode] LIKE @Stem + N'%'
                        UNION ALL
                        SELECT TRY_CONVERT(bigint, SUBSTRING([SourceCode], LEN(@Stem) + 1, 64))
                        FROM [NotificationHistory]
                        WHERE [Kind] = 'Alert' AND [SourceCode] LIKE @Stem + N'%'
                    ) AS [codes]), @StartingNumber - 1);
                IF @MaxSequence < @StartingNumber - 1 SET @MaxSequence = @StartingNumber - 1;

                ;WITH [DistinctTargets] AS
                (
                    SELECT [SourceId], MIN([CreatedAtUtc]) AS [FirstRecordedAt]
                    FROM [NotificationHistory]
                    WHERE [Kind] = 'Alert' AND [SourceCode] NOT LIKE @Stem + N'%'
                    GROUP BY [SourceId]
                ),
                [Targets] AS
                (
                    SELECT [SourceId], ROW_NUMBER() OVER (ORDER BY [FirstRecordedAt], [SourceId]) AS [RowNumber]
                    FROM [DistinctTargets]
                )
                UPDATE [history]
                SET [history].[SourceCode] = @Stem
                    + CASE WHEN LEN([number].[Value]) < @Padding
                        THEN REPLICATE(N'0', @Padding - LEN([number].[Value])) + [number].[Value]
                        ELSE [number].[Value] END
                FROM [NotificationHistory] AS [history]
                INNER JOIN [Targets] AS [target] ON [target].[SourceId] = [history].[SourceId]
                CROSS APPLY (SELECT CONVERT(nvarchar(20), @MaxSequence + [target].[RowNumber]) AS [Value]) AS [number]
                WHERE [history].[Kind] = 'Alert';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
