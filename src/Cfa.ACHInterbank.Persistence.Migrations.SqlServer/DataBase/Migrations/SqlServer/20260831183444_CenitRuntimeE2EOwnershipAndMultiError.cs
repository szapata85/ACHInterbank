using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class CenitRuntimeE2EOwnershipAndMultiError : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CompanyEntryDescription",
                columns: new[] { "Id", "Description", "IsActive", "StandardEntryClassCode", "Term" },
                values: new object[] { 39, "Pagos corporativos CENIT con múltiples adendas.", true, "CTX", "CORPORATE" });

            migrationBuilder.DropIndex(
                name: "IX_CenitChamberResponses_ClearingHouseId_SourceResponseId",
                table: "CenitChamberResponses");

            migrationBuilder.AddColumn<string>(
                name: "AchCycleId",
                table: "CenitChamberResponses",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemCount",
                table: "CenitChamberResponses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ItemSequence",
                table: "CenitChamberResponses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "MessageCreatedAtUtc",
                table: "CenitChamberResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageGroupId",
                table: "CenitChamberResponses",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageStatus",
                table: "CenitChamberResponses",
                type: "nvarchar(35)",
                maxLength: 35,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginatingSender",
                table: "CenitChamberResponses",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XmlNamespace",
                table: "CenitChamberResponses",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CenitChamberResponses_AchCycleId_ReceivedAtUtc",
                table: "CenitChamberResponses",
                columns: new[] { "AchCycleId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CenitChamberResponses_ClearingHouseId_SourceResponseId_ItemSequence",
                table: "CenitChamberResponses",
                columns: new[] { "ClearingHouseId", "SourceResponseId", "ItemSequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CenitChamberResponses_AchCycles_AchCycleId",
                table: "CenitChamberResponses",
                column: "AchCycleId",
                principalTable: "AchCycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CompanyEntryDescription",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DropForeignKey(
                name: "FK_CenitChamberResponses_AchCycles_AchCycleId",
                table: "CenitChamberResponses");

            migrationBuilder.DropIndex(
                name: "IX_CenitChamberResponses_AchCycleId_ReceivedAtUtc",
                table: "CenitChamberResponses");

            migrationBuilder.DropIndex(
                name: "IX_CenitChamberResponses_ClearingHouseId_SourceResponseId_ItemSequence",
                table: "CenitChamberResponses");

            migrationBuilder.DropColumn(
                name: "AchCycleId",
                table: "CenitChamberResponses");

            migrationBuilder.DropColumn(
                name: "ItemCount",
                table: "CenitChamberResponses");

            migrationBuilder.DropColumn(
                name: "ItemSequence",
                table: "CenitChamberResponses");

            migrationBuilder.DropColumn(
                name: "MessageCreatedAtUtc",
                table: "CenitChamberResponses");

            migrationBuilder.DropColumn(
                name: "MessageGroupId",
                table: "CenitChamberResponses");

            migrationBuilder.DropColumn(
                name: "MessageStatus",
                table: "CenitChamberResponses");

            migrationBuilder.DropColumn(
                name: "OriginatingSender",
                table: "CenitChamberResponses");

            migrationBuilder.DropColumn(
                name: "XmlNamespace",
                table: "CenitChamberResponses");

            migrationBuilder.CreateIndex(
                name: "IX_CenitChamberResponses_ClearingHouseId_SourceResponseId",
                table: "CenitChamberResponses",
                columns: new[] { "ClearingHouseId", "SourceResponseId" },
                unique: true);
        }
    }
}
