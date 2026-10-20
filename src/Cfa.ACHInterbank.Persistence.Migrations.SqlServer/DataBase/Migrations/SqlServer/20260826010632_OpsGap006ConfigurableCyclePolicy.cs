using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
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
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsDebitPrenotification",
                table: "ClearingHouseCycleConfigs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsMonetaryCredit",
                table: "ClearingHouseCycleConfigs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsMonetaryDebit",
                table: "ClearingHouseCycleConfigs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsReturn",
                table: "ClearingHouseCycleConfigs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsReturnOfReturn",
                table: "ClearingHouseCycleConfigs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OutputReleaseTime",
                table: "ClearingHouseCycleConfigs",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "PolicyVersion",
                table: "ClearingHouseCycleConfigs",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "LEGACY");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OutputReleaseTime",
                table: "AchCycles",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.Sql(
                "UPDATE [ClearingHouseCycleConfigs] SET [OutputReleaseTime] = [EndTime]; " +
                "UPDATE [AchCycles] SET [OutputReleaseTime] = [EndTime];");

            migrationBuilder.Sql(
                "UPDATE c SET [AllowsMonetaryDebit] = 0 " +
                "FROM [ClearingHouseCycleConfigs] c " +
                "INNER JOIN [ClearingHouses] h ON h.[Id] = c.[ClearingHouseId] " +
                "WHERE UPPER(h.[Code]) = 'ACHCOL' " +
                "AND UPPER(REPLACE(REPLACE(c.[CycleName], ' ', ''), '-', '')) IN ('CICLO5', 'CYCLE5');");

            migrationBuilder.Sql(
                "UPDATE c SET [AllowsReturn] = 0, [AllowsReturnOfReturn] = 0 " +
                "FROM [ClearingHouseCycleConfigs] c " +
                "INNER JOIN [ClearingHouses] h ON h.[Id] = c.[ClearingHouseId] " +
                "WHERE UPPER(h.[Code]) = 'CENIT' " +
                "AND UPPER(REPLACE(REPLACE(c.[CycleName], ' ', ''), '-', '')) IN ('CICLO1', 'CYCLE1'); " +
                "UPDATE c SET [AllowsMonetaryCredit] = 0, [AllowsMonetaryDebit] = 0, " +
                "[AllowsCreditPrenotification] = 0, [AllowsDebitPrenotification] = 0, " +
                "[OutputReleaseTime] = CAST('19:15:00' AS time) " +
                "FROM [ClearingHouseCycleConfigs] c " +
                "INNER JOIN [ClearingHouses] h ON h.[Id] = c.[ClearingHouseId] " +
                "WHERE UPPER(h.[Code]) = 'CENIT' " +
                "AND UPPER(REPLACE(REPLACE(c.[CycleName], ' ', ''), '-', '')) IN ('CICLO5', 'CYCLE5');");
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
