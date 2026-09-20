using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceLatePenalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LatePenaltyAmount",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LatePenaltyDays",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatePenaltyAmount",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "LatePenaltyDays",
                schema: "Finance",
                table: "FinancialAccounts");
        }
    }
}
