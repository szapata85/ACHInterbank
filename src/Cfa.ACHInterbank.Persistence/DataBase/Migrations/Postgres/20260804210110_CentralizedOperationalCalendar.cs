using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class CentralizedOperationalCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ClearingHouseSpecialDates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "timezone('utc', now())");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ClearingHouseSpecialDates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "timezone('utc', now())");

            migrationBuilder.AddColumn<DateOnly>(
                name: "CommemorativeDate",
                table: "BankHolidays",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "BankHolidays",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "timezone('utc', now())");

            migrationBuilder.AddColumn<int>(
                name: "EffectiveFromYear",
                table: "BankHolidays",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemGenerated",
                table: "BankHolidays",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegalOrigin",
                table: "BankHolidays",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RuleCode",
                table: "BankHolidays",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RuleKind",
                table: "BankHolidays",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "BankHolidays",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "timezone('utc', now())");

            migrationBuilder.AddColumn<int>(
                name: "CalendarDeferralCount",
                table: "AchCycles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CalendarDeferralReason",
                table: "AchCycles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CalendarDeferredAtUtc",
                table: "AchCycles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalProcessingDate",
                table: "AchCycles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankHolidays_EffectiveDate",
                table: "BankHolidays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "UX_BankHolidays_LegalRule",
                table: "BankHolidays",
                columns: new[] { "RuleCode", "CommemorativeDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankHolidays_EffectiveDate",
                table: "BankHolidays");

            migrationBuilder.DropIndex(
                name: "UX_BankHolidays_LegalRule",
                table: "BankHolidays");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ClearingHouseSpecialDates");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ClearingHouseSpecialDates");

            migrationBuilder.DropColumn(
                name: "CommemorativeDate",
                table: "BankHolidays");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BankHolidays");

            migrationBuilder.DropColumn(
                name: "EffectiveFromYear",
                table: "BankHolidays");

            migrationBuilder.DropColumn(
                name: "IsSystemGenerated",
                table: "BankHolidays");

            migrationBuilder.DropColumn(
                name: "LegalOrigin",
                table: "BankHolidays");

            migrationBuilder.DropColumn(
                name: "RuleCode",
                table: "BankHolidays");

            migrationBuilder.DropColumn(
                name: "RuleKind",
                table: "BankHolidays");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BankHolidays");

            migrationBuilder.DropColumn(
                name: "CalendarDeferralCount",
                table: "AchCycles");

            migrationBuilder.DropColumn(
                name: "CalendarDeferralReason",
                table: "AchCycles");

            migrationBuilder.DropColumn(
                name: "CalendarDeferredAtUtc",
                table: "AchCycles");

            migrationBuilder.DropColumn(
                name: "OriginalProcessingDate",
                table: "AchCycles");
        }
    }
}
