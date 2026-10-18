using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddAchCycleStageWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddStageColumns(migrationBuilder, "ClearingHouseCycleConfigs");
            AddStageColumns(migrationBuilder, "AchCycles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropStageColumns(migrationBuilder, "AchCycles");
            DropStageColumns(migrationBuilder, "ClearingHouseCycleConfigs");
        }

        private static void AddStageColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "CompensationStatementStartTime",
                table: table,
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CompensationStatementEndTime",
                table: table,
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CompensationPaymentStartTime",
                table: table,
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CompensationPaymentEndTime",
                table: table,
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OutputReleaseStartTime",
                table: table,
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OutputReleaseEndTime",
                table: table,
                type: "interval",
                nullable: true);
        }

        private static void DropStageColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.DropColumn(name: "CompensationStatementStartTime", table: table);
            migrationBuilder.DropColumn(name: "CompensationStatementEndTime", table: table);
            migrationBuilder.DropColumn(name: "CompensationPaymentStartTime", table: table);
            migrationBuilder.DropColumn(name: "CompensationPaymentEndTime", table: table);
            migrationBuilder.DropColumn(name: "OutputReleaseStartTime", table: table);
            migrationBuilder.DropColumn(name: "OutputReleaseEndTime", table: table);
        }
    }
}
