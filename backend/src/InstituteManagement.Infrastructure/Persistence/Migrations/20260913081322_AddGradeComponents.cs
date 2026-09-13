using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AssignmentMaximum",
                table: "GradeRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 20m);

            migrationBuilder.AddColumn<decimal>(
                name: "AssignmentScore",
                table: "GradeRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AttendanceMaximum",
                table: "GradeRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<decimal>(
                name: "AttendanceScore",
                table: "GradeRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalExamMaximum",
                table: "GradeRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 50m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalExamScore",
                table: "GradeRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MidtermMaximum",
                table: "GradeRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 20m);

            migrationBuilder.AddColumn<decimal>(
                name: "MidtermScore",
                table: "GradeRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE [GradeRecords]
                SET [AttendanceScore] = ROUND([Score] * 0.10, 2),
                    [AssignmentScore] = ROUND([Score] * 0.20, 2),
                    [MidtermScore] = ROUND([Score] * 0.20, 2),
                    [FinalExamScore] = [Score] - ROUND([Score] * 0.10, 2) - ROUND([Score] * 0.20, 2) - ROUND([Score] * 0.20, 2);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentMaximum",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "AssignmentScore",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "AttendanceMaximum",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "AttendanceScore",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "FinalExamMaximum",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "FinalExamScore",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "MidtermMaximum",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "MidtermScore",
                table: "GradeRecords");
        }
    }
}
