using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandFinanceAccountsAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentPayments_AcademicYear_Semester_Status_DueOn",
                schema: "Finance",
                table: "StudentPayments");

            migrationBuilder.DropIndex(
                name: "IX_StudentPayments_PaymentCode_AcademicYear_Semester",
                schema: "Finance",
                table: "StudentPayments");

            migrationBuilder.DropIndex(
                name: "IX_StudentPayments_StudentEnrollmentId",
                schema: "Finance",
                table: "StudentPayments");

            migrationBuilder.DropIndex(
                name: "IX_StudentPayments_StudentId_AcademicYear_Semester",
                schema: "Finance",
                table: "StudentPayments");

            migrationBuilder.RenameTable(
                name: "StudentPayments",
                schema: "Finance",
                newName: "FinancialAccounts",
                newSchema: "Finance");

            migrationBuilder.RenameColumn(
                name: "PaymentCode",
                schema: "Finance",
                table: "FinancialAccounts",
                newName: "FinancialAccountCode");

            migrationBuilder.RenameColumn(
                name: "AmountDue",
                schema: "Finance",
                table: "FinancialAccounts",
                newName: "TuitionFee");

            migrationBuilder.AddColumn<decimal>(
                name: "OtherFee",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AdjustmentAmount",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AdjustmentReason",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "Finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalSchema: "Finance",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                UPDATE [Finance].[FinancialAccounts]
                SET [FinancialAccountCode] = CONCAT('FIN-', SUBSTRING([FinancialAccountCode], 5, 60))
                WHERE [FinancialAccountCode] LIKE 'PAY-%';

                INSERT INTO [Finance].[Payments]
                    ([Id], [PaymentCode], [FinancialAccountId], [Amount], [Method], [Status], [TransactionReference], [PaidAtUtc], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT
                    NEWID(),
                    CONCAT('P-', RIGHT(CONCAT('00000', ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc], [Id])), 5)),
                    [Id],
                    [TuitionFee],
                    'Other',
                    'Completed',
                    CASE WHEN NULLIF([ConfirmationMethod], '') IS NULL THEN 'Migrated payment' ELSE [ConfirmationMethod] END,
                    COALESCE([PaidAtUtc], [UpdatedAtUtc], [CreatedAtUtc]),
                    [CreatedAtUtc],
                    [UpdatedAtUtc]
                FROM [Finance].[FinancialAccounts]
                WHERE [Status] = 'Paid' AND [TuitionFee] > 0;
                """);

            migrationBuilder.DropColumn(
                name: "ConfirmationMethod",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_AcademicYear_Semester_Status_DueOn",
                schema: "Finance",
                table: "FinancialAccounts",
                columns: new[] { "AcademicYear", "Semester", "Status", "DueOn" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_FinancialAccountCode_AcademicYear_Semester",
                schema: "Finance",
                table: "FinancialAccounts",
                columns: new[] { "FinancialAccountCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_StudentEnrollmentId",
                schema: "Finance",
                table: "FinancialAccounts",
                column: "StudentEnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_StudentId_AcademicYear_Semester",
                schema: "Finance",
                table: "FinancialAccounts",
                columns: new[] { "StudentId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_FinancialAccountId_Status_PaidAtUtc",
                schema: "Finance",
                table: "Payments",
                columns: new[] { "FinancialAccountId", "Status", "PaidAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentCode",
                schema: "Finance",
                table: "Payments",
                column: "PaymentCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfirmationMethod",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE account
                SET
                    [ConfirmationMethod] = COALESCE(LEFT(payment.[TransactionReference], 32), ''),
                    [PaidAtUtc] = payment.[PaidAtUtc]
                FROM [Finance].[FinancialAccounts] account
                OUTER APPLY (
                    SELECT TOP (1) [TransactionReference], [PaidAtUtc]
                    FROM [Finance].[Payments]
                    WHERE [FinancialAccountId] = account.[Id] AND [Status] = 'Completed'
                    ORDER BY [PaidAtUtc] DESC, [CreatedAtUtc] DESC
                ) payment;
                """);

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "Finance");

            migrationBuilder.DropIndex(
                name: "IX_FinancialAccounts_AcademicYear_Semester_Status_DueOn",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropIndex(
                name: "IX_FinancialAccounts_FinancialAccountCode_AcademicYear_Semester",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropIndex(
                name: "IX_FinancialAccounts_StudentEnrollmentId",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropIndex(
                name: "IX_FinancialAccounts_StudentId_AcademicYear_Semester",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "AdjustmentAmount",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "AdjustmentReason",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "OtherFee",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.RenameColumn(
                name: "FinancialAccountCode",
                schema: "Finance",
                table: "FinancialAccounts",
                newName: "PaymentCode");

            migrationBuilder.RenameColumn(
                name: "TuitionFee",
                schema: "Finance",
                table: "FinancialAccounts",
                newName: "AmountDue");

            migrationBuilder.Sql(
                """
                UPDATE [Finance].[FinancialAccounts]
                SET [PaymentCode] = CONCAT('PAY-', SUBSTRING([PaymentCode], 5, 60))
                WHERE [PaymentCode] LIKE 'FIN-%';
                """);

            migrationBuilder.RenameTable(
                name: "FinancialAccounts",
                schema: "Finance",
                newName: "StudentPayments",
                newSchema: "Finance");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPayments_AcademicYear_Semester_Status_DueOn",
                schema: "Finance",
                table: "StudentPayments",
                columns: new[] { "AcademicYear", "Semester", "Status", "DueOn" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentPayments_PaymentCode_AcademicYear_Semester",
                schema: "Finance",
                table: "StudentPayments",
                columns: new[] { "PaymentCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPayments_StudentEnrollmentId",
                schema: "Finance",
                table: "StudentPayments",
                column: "StudentEnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPayments_StudentId_AcademicYear_Semester",
                schema: "Finance",
                table: "StudentPayments",
                columns: new[] { "StudentId", "AcademicYear", "Semester" },
                unique: true);
        }
    }
}
