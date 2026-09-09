using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementReadState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "Announcements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE [alert]
                SET [alert].[IsRead] = [notification].[IsRead]
                FROM [Announcements] AS [alert]
                INNER JOIN [Notifications] AS [notification] ON [notification].[Id] = [alert].[NotificationId];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Announcements");
        }
    }
}
