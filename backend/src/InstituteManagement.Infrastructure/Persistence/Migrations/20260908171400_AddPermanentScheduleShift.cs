using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermanentScheduleShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Shift",
                table: "ScheduleEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Morning");

            migrationBuilder.Sql(
                """
                UPDATE [ScheduleEntries]
                SET [Shift] = CASE
                    WHEN [DayOfWeek] IN (0, 6) THEN N'Weekend'
                    WHEN [StartsAt] < CAST('13:00' AS time) THEN N'Morning'
                    WHEN [StartsAt] < CAST('17:00' AS time) THEN N'Afternoon'
                    ELSE N'Evening'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Shift",
                table: "ScheduleEntries");
        }
    }
}
