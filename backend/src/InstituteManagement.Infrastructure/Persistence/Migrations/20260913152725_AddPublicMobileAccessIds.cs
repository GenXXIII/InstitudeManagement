using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicMobileAccessIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Teachers",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Students",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE [Teachers]
                SET [PublicId] = N'TEA-' + UPPER(LEFT(REPLACE(CONVERT(nvarchar(36), [Id]), N'-', N''), 12));

                UPDATE [Students]
                SET [PublicId] = N'STU-' + UPPER(LEFT(REPLACE(CONVERT(nvarchar(36), [Id]), N'-', N''), 12));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_PublicId",
                table: "Teachers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_PublicId",
                table: "Students",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teachers_PublicId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Students_PublicId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Students");
        }
    }
}
