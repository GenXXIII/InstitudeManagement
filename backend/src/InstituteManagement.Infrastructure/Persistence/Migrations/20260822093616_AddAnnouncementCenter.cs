using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SystemSettingCode",
                table: "SystemSettings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE [SystemSettings]
                SET [SystemSettingCode] = 'SET-' + REPLACE(CONVERT(nvarchar(36), [Id]), '-', '')
                WHERE [SystemSettingCode] = '';
                """);

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnnouncementCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationHistoryCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_SystemSettingCode",
                table: "SystemSettings",
                column: "SystemSettingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_AnnouncementCode",
                table: "Announcements",
                column: "AnnouncementCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsActive_CreatedAtUtc",
                table: "Announcements",
                columns: new[] { "IsActive", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_Kind_CreatedAtUtc",
                table: "NotificationHistory",
                columns: new[] { "Kind", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_NotificationHistoryCode",
                table: "NotificationHistory",
                column: "NotificationHistoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_SourceId",
                table: "NotificationHistory",
                column: "SourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "NotificationHistory");

            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_SystemSettingCode",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SystemSettingCode",
                table: "SystemSettings");
        }
    }
}
