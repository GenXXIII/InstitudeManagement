using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherAttendanceSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('ClassSessionRecords', 'TeacherAttendanceStatus') IS NULL
                    ALTER TABLE [ClassSessionRecords] ADD [TeacherAttendanceStatus] nvarchar(32) NOT NULL CONSTRAINT [DF_ClassSessionRecords_TeacherAttendanceStatus] DEFAULT 'Present';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeacherAttendanceStatus",
                table: "ClassSessionRecords");
        }
    }
}
