using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCurrentCourseAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE assignment
                FROM [Enrollment].[CourseAssignments] assignment
                WHERE NOT EXISTS (
                    SELECT 1 FROM [ScheduleEntries] entry
                    WHERE entry.[CourseId] = assignment.[CourseId] AND entry.[Status] <> N'Cancelled');

                UPDATE assignment
                SET assignment.[YearLevel] = schedule.[YearLevel], assignment.[UpdatedAtUtc] = SYSUTCDATETIME()
                FROM [Enrollment].[CourseAssignments] assignment
                INNER JOIN (
                    SELECT [CourseId], MIN([YearLevel]) AS [YearLevel]
                    FROM [ScheduleEntries]
                    WHERE [Status] <> N'Cancelled'
                    GROUP BY [CourseId]
                ) schedule ON schedule.[CourseId] = assignment.[CourseId];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
