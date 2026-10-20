using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class EnforceFinancialIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "IncomingNachaProcessingEvents" WHERE "IncomingNachaFileIngestionId1" IS NOT NULL) THEN
                        RAISE EXCEPTION 'La FK sombra IncomingNachaFileIngestionId1 contiene datos y requiere correccion manual antes de migrar.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "AchBatches"
                        WHERE "TotalDebitAmount" <> trunc("TotalDebitAmount", 2)
                           OR "TotalCreditAmount" <> trunc("TotalCreditAmount", 2)
                        UNION ALL
                        SELECT 1
                        FROM "AchTransactions"
                        WHERE "Amount" <> trunc("Amount", 2)
                        UNION ALL
                        SELECT 1
                        FROM "EntryDetails"
                        WHERE "Amount" IS NOT NULL AND "Amount"::numeric <> trunc("Amount"::numeric, 2)
                        UNION ALL
                        SELECT 1
                        FROM "BatchControls"
                        WHERE "TotalDebitAmount"::numeric <> trunc("TotalDebitAmount"::numeric, 2)
                           OR "TotalCreditAmount"::numeric <> trunc("TotalCreditAmount"::numeric, 2)
                        UNION ALL
                        SELECT 1
                        FROM "FileControls"
                        WHERE "TotalDebitAmount"::numeric <> trunc("TotalDebitAmount"::numeric, 2)
                           OR "TotalCreditAmount"::numeric <> trunc("TotalCreditAmount"::numeric, 2)) THEN
                        RAISE EXCEPTION 'Existen montos fuera de escala monetaria de dos decimales; la migracion se cancela para evitar redondeo o truncamiento.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_I~",
                table: "IncomingNachaProcessingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_~1",
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
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "money");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCreditAmount",
                table: "FileControls",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "money");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "EntryDetails",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "money",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDebitAmount",
                table: "BatchControls",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "money");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCreditAmount",
                table: "BatchControls",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "money");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "AchTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDebitAmount",
                table: "AchBatches",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCreditAmount",
                table: "AchBatches",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_I~",
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
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "FileControls"
                        WHERE abs("TotalDebitAmount"::numeric) > 922337203685477.5807
                           OR abs("TotalCreditAmount"::numeric) > 922337203685477.5807
                        UNION ALL
                        SELECT 1
                        FROM "EntryDetails"
                        WHERE "Amount" IS NOT NULL AND abs("Amount"::numeric) > 922337203685477.5807
                        UNION ALL
                        SELECT 1
                        FROM "BatchControls"
                        WHERE abs("TotalDebitAmount"::numeric) > 922337203685477.5807
                           OR abs("TotalCreditAmount"::numeric) > 922337203685477.5807) THEN
                        RAISE EXCEPTION 'El rollback a money excederia el rango historico y se cancela para preservar los montos.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_I~",
                table: "IncomingNachaProcessingEvents");

            migrationBuilder.AddColumn<Guid>(
                name: "IncomingNachaFileIngestionId1",
                table: "IncomingNachaProcessingEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDebitAmount",
                table: "FileControls",
                type: "money",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCreditAmount",
                table: "FileControls",
                type: "money",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "EntryDetails",
                type: "money",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDebitAmount",
                table: "BatchControls",
                type: "money",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCreditAmount",
                table: "BatchControls",
                type: "money",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "AchTransactions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDebitAmount",
                table: "AchBatches",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCreditAmount",
                table: "AchBatches",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaProcessingEvents_IncomingNachaFileIngestionId1",
                table: "IncomingNachaProcessingEvents",
                column: "IncomingNachaFileIngestionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_I~",
                table: "IncomingNachaProcessingEvents",
                column: "IncomingNachaFileIngestionId",
                principalTable: "IncomingNachaFileIngestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_~1",
                table: "IncomingNachaProcessingEvents",
                column: "IncomingNachaFileIngestionId1",
                principalTable: "IncomingNachaFileIngestions",
                principalColumn: "Id");
        }
    }
}
