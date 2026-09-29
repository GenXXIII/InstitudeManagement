using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StudentEnrollmentPublicIdJourney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EnrollmentSemesters_PublicId",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.Sql(
                """
                ;WITH [RankedPublicIds] AS (
                    SELECT
                        [StudentAcademicEnrollmentId],
                        [PublicId],
                        ROW_NUMBER() OVER (
                            PARTITION BY [StudentAcademicEnrollmentId]
                            ORDER BY
                                CASE WHEN NULLIF(LTRIM(RTRIM([PublicId])), N'') IS NULL THEN 1 ELSE 0 END,
                                [CreatedAtUtc],
                                [Id]
                        ) AS [PublicIdOrder]
                    FROM [Enrollment].[EnrollmentSemesters]
                ),
                [JourneyPublicIds] AS (
                    SELECT [StudentAcademicEnrollmentId], [PublicId]
                    FROM [RankedPublicIds]
                    WHERE [PublicIdOrder] = 1
                )
                UPDATE semester
                SET [PublicId] = journey.[PublicId]
                FROM [Enrollment].[EnrollmentSemesters] semester
                INNER JOIN [JourneyPublicIds] journey
                    ON journey.[StudentAcademicEnrollmentId] = semester.[StudentAcademicEnrollmentId];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentSemesters_PublicId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                columns: new[] { "PublicId", "AcademicYear", "Semester" },
                unique: true,
                filter: "[PublicId] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EnrollmentSemesters_PublicId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.Sql(
                """
                ;WITH [DuplicatePublicIds] AS (
                    SELECT
                        [Id],
                        [PublicId],
                        ROW_NUMBER() OVER (
                            PARTITION BY [PublicId]
                            ORDER BY [CreatedAtUtc], [Id]
                        ) AS [PublicIdOccurrence]
                    FROM [Enrollment].[EnrollmentSemesters]
                    WHERE [PublicId] <> N''
                )
                UPDATE semester
                SET [PublicId] =
                    CASE
                        WHEN LEN(semester.[PublicId]) >= 33
                            THEN LEFT(semester.[PublicId], LEN(semester.[PublicId]) - 32)
                        ELSE N'STU-'
                    END
                    + REPLACE(UPPER(CONVERT(nvarchar(36), semester.[Id])), N'-', N'')
                FROM [Enrollment].[EnrollmentSemesters] semester
                INNER JOIN [DuplicatePublicIds] duplicate ON duplicate.[Id] = semester.[Id]
                WHERE duplicate.[PublicIdOccurrence] > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentSemesters_PublicId",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                column: "PublicId",
                unique: true,
                filter: "[PublicId] <> ''");
        }
    }
}
