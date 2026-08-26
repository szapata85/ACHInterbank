using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class OpsGap006ConfigurableCyclePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowsCreditPrenotification",
                table: "ClearingHouseCycleConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsDebitPrenotification",
                table: "ClearingHouseCycleConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsMonetaryCredit",
                table: "ClearingHouseCycleConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsMonetaryDebit",
                table: "ClearingHouseCycleConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsReturn",
                table: "ClearingHouseCycleConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsReturnOfReturn",
                table: "ClearingHouseCycleConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OutputReleaseTime",
                table: "ClearingHouseCycleConfigs",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "PolicyVersion",
                table: "ClearingHouseCycleConfigs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "LEGACY");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OutputReleaseTime",
                table: "AchCycles",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.Sql(
                "UPDATE \"ClearingHouseCycleConfigs\" SET \"OutputReleaseTime\" = \"EndTime\"; " +
                "UPDATE \"AchCycles\" SET \"OutputReleaseTime\" = \"EndTime\";");

            migrationBuilder.Sql(
                "UPDATE \"ClearingHouseCycleConfigs\" c SET \"AllowsMonetaryDebit\" = FALSE " +
                "FROM \"ClearingHouses\" h " +
                "WHERE h.\"Id\" = c.\"ClearingHouseId\" AND UPPER(h.\"Code\") = 'ACHCOL' " +
                "AND UPPER(REPLACE(REPLACE(c.\"CycleName\", ' ', ''), '-', '')) IN ('CICLO5', 'CYCLE5');");

            migrationBuilder.Sql(
                "UPDATE \"ClearingHouseCycleConfigs\" c " +
                "SET \"AllowsReturn\" = FALSE, \"AllowsReturnOfReturn\" = FALSE " +
                "FROM \"ClearingHouses\" h " +
                "WHERE h.\"Id\" = c.\"ClearingHouseId\" AND UPPER(h.\"Code\") = 'CENIT' " +
                "AND UPPER(REPLACE(REPLACE(c.\"CycleName\", ' ', ''), '-', '')) IN ('CICLO1', 'CYCLE1'); " +
                "UPDATE \"ClearingHouseCycleConfigs\" c " +
                "SET \"AllowsMonetaryCredit\" = FALSE, \"AllowsMonetaryDebit\" = FALSE, " +
                "\"AllowsCreditPrenotification\" = FALSE, \"AllowsDebitPrenotification\" = FALSE, " +
                "\"OutputReleaseTime\" = INTERVAL '19 hours 15 minutes' " +
                "FROM \"ClearingHouses\" h " +
                "WHERE h.\"Id\" = c.\"ClearingHouseId\" AND UPPER(h.\"Code\") = 'CENIT' " +
                "AND UPPER(REPLACE(REPLACE(c.\"CycleName\", ' ', ''), '-', '')) IN ('CICLO5', 'CYCLE5');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowsCreditPrenotification",
                table: "ClearingHouseCycleConfigs");

            migrationBuilder.DropColumn(
                name: "AllowsDebitPrenotification",
                table: "ClearingHouseCycleConfigs");

            migrationBuilder.DropColumn(
                name: "AllowsMonetaryCredit",
                table: "ClearingHouseCycleConfigs");

            migrationBuilder.DropColumn(
                name: "AllowsMonetaryDebit",
                table: "ClearingHouseCycleConfigs");

            migrationBuilder.DropColumn(
                name: "AllowsReturn",
                table: "ClearingHouseCycleConfigs");

            migrationBuilder.DropColumn(
                name: "AllowsReturnOfReturn",
                table: "ClearingHouseCycleConfigs");

            migrationBuilder.DropColumn(
                name: "OutputReleaseTime",
                table: "ClearingHouseCycleConfigs");

            migrationBuilder.DropColumn(
                name: "PolicyVersion",
                table: "ClearingHouseCycleConfigs");

            migrationBuilder.DropColumn(
                name: "OutputReleaseTime",
                table: "AchCycles");
        }
    }
}
