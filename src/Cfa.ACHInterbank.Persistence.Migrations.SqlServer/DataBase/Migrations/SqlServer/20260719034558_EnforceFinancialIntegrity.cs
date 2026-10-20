using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class EnforceFinancialIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [IncomingNachaProcessingEvents] WHERE [IncomingNachaFileIngestionId1] IS NOT NULL)
                    THROW 50000, 'La FK sombra IncomingNachaFileIngestionId1 contiene datos y requiere correccion manual antes de migrar.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM [AchBatches]
                    WHERE [TotalDebitAmount] <> CONVERT(decimal(18,2), [TotalDebitAmount])
                       OR [TotalCreditAmount] <> CONVERT(decimal(18,2), [TotalCreditAmount])
                    UNION ALL
                    SELECT 1
                    FROM [AchTransactions]
                    WHERE [Amount] <> CONVERT(decimal(18,2), [Amount])
                    UNION ALL
                    SELECT 1
                    FROM [EntryDetails]
                    WHERE [Amount] IS NOT NULL AND [Amount] <> CONVERT(decimal(18,2), [Amount])
                    UNION ALL
                    SELECT 1
                    FROM [BatchControls]
                    WHERE [TotalDebitAmount] <> CONVERT(decimal(18,2), [TotalDebitAmount])
                       OR [TotalCreditAmount] <> CONVERT(decimal(18,2), [TotalCreditAmount])
                    UNION ALL
                    SELECT 1
                    FROM [FileControls]
                    WHERE [TotalDebitAmount] <> CONVERT(decimal(18,2), [TotalDebitAmount])
                       OR [TotalCreditAmount] <> CONVERT(decimal(18,2), [TotalCreditAmount]))
                    THROW 50001, 'Existen montos fuera de escala monetaria de dos decimales; la migracion se cancela para evitar redondeo o truncamiento.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_IncomingNachaFileIngestionId",
                table: "IncomingNachaProcessingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_IncomingNachaFileIngestionId1",
                table: "IncomingNachaProcessingEvents");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaProcessingEvents_IncomingNachaFileIngestionId1",
                table: "IncomingNachaProcessingEvents");

            migrationBuilder.DropColumn(
                name: "IncomingNachaFileIngestionId1",
                table: "IncomingNachaProcessingEvents");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDebitAmount",
                table: "FileControls",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "money");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCreditAmount",
                table: "FileControls",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "money");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "EntryDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "money",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDebitAmount",
                table: "BatchControls",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "money");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCreditAmount",
                table: "BatchControls",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "money");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_IncomingNachaFileIngestionId",
                table: "IncomingNachaProcessingEvents",
                column: "IncomingNachaFileIngestionId",
                principalTable: "IncomingNachaFileIngestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM [FileControls]
                    WHERE ABS([TotalDebitAmount]) > 922337203685477.5807
                       OR ABS([TotalCreditAmount]) > 922337203685477.5807
                    UNION ALL
                    SELECT 1
                    FROM [EntryDetails]
                    WHERE [Amount] IS NOT NULL AND ABS([Amount]) > 922337203685477.5807
                    UNION ALL
                    SELECT 1
                    FROM [BatchControls]
                    WHERE ABS([TotalDebitAmount]) > 922337203685477.5807
                       OR ABS([TotalCreditAmount]) > 922337203685477.5807)
                    THROW 50002, 'El rollback a money excederia el rango historico y se cancela para preservar los montos.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_IncomingNachaFileIngestionId",
                table: "IncomingNachaProcessingEvents");

            migrationBuilder.AddColumn<Guid>(
                name: "IncomingNachaFileIngestionId1",
                table: "IncomingNachaProcessingEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDebitAmount",
                table: "FileControls",
                type: "money",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCreditAmount",
                table: "FileControls",
                type: "money",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "EntryDetails",
                type: "money",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDebitAmount",
                table: "BatchControls",
                type: "money",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCreditAmount",
                table: "BatchControls",
                type: "money",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaProcessingEvents_IncomingNachaFileIngestionId1",
                table: "IncomingNachaProcessingEvents",
                column: "IncomingNachaFileIngestionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_IncomingNachaFileIngestionId",
                table: "IncomingNachaProcessingEvents",
                column: "IncomingNachaFileIngestionId",
                principalTable: "IncomingNachaFileIngestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_IncomingNachaFileIngestionId1",
                table: "IncomingNachaProcessingEvents",
                column: "IncomingNachaFileIngestionId1",
                principalTable: "IncomingNachaFileIngestions",
                principalColumn: "Id");
        }
    }
}
