using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class CenitReturnOfReturn2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReturnOfReturnFlows_SourceReturnTransactionId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.AlterColumn<int>(
                name: "SourceReturnTransactionId",
                table: "ReturnOfReturnFlows",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "ReturnOfReturnFlows",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<int>(
                name: "OriginalTransactionId",
                table: "ReturnOfReturnFlows",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ParentIncomingReturnStateEventId",
                table: "ReturnOfReturnFlows",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentOutgoingReturnGeneratedId",
                table: "ReturnOfReturnFlows",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_OriginalTransactionId",
                table: "ReturnOfReturnFlows",
                column: "OriginalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_ParentIncomingReturnStateEventId",
                table: "ReturnOfReturnFlows",
                column: "ParentIncomingReturnStateEventId",
                unique: true,
                filter: "[ParentIncomingReturnStateEventId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_ParentOutgoingReturnGeneratedId",
                table: "ReturnOfReturnFlows",
                column: "ParentOutgoingReturnGeneratedId",
                unique: true,
                filter: "[ParentOutgoingReturnGeneratedId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_SourceReturnTransactionId",
                table: "ReturnOfReturnFlows",
                column: "SourceReturnTransactionId",
                unique: true,
                filter: "[SourceReturnTransactionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOfReturnFlows_AchReturnsGenerated_ParentOutgoingReturnGeneratedId",
                table: "ReturnOfReturnFlows",
                column: "ParentOutgoingReturnGeneratedId",
                principalTable: "AchReturnsGenerated",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOfReturnFlows_AchTransactionStateEvents_ParentIncomingReturnStateEventId",
                table: "ReturnOfReturnFlows",
                column: "ParentIncomingReturnStateEventId",
                principalTable: "AchTransactionStateEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOfReturnFlows_AchTransactions_OriginalTransactionId",
                table: "ReturnOfReturnFlows",
                column: "OriginalTransactionId",
                principalTable: "AchTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOfReturnFlows_AchReturnsGenerated_ParentOutgoingReturnGeneratedId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOfReturnFlows_AchTransactionStateEvents_ParentIncomingReturnStateEventId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOfReturnFlows_AchTransactions_OriginalTransactionId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropIndex(
                name: "IX_ReturnOfReturnFlows_OriginalTransactionId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropIndex(
                name: "IX_ReturnOfReturnFlows_ParentIncomingReturnStateEventId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropIndex(
                name: "IX_ReturnOfReturnFlows_ParentOutgoingReturnGeneratedId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropIndex(
                name: "IX_ReturnOfReturnFlows_SourceReturnTransactionId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropColumn(
                name: "OriginalTransactionId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropColumn(
                name: "ParentIncomingReturnStateEventId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropColumn(
                name: "ParentOutgoingReturnGeneratedId",
                table: "ReturnOfReturnFlows");

            migrationBuilder.AlterColumn<int>(
                name: "SourceReturnTransactionId",
                table: "ReturnOfReturnFlows",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_SourceReturnTransactionId",
                table: "ReturnOfReturnFlows",
                column: "SourceReturnTransactionId",
                unique: true);
        }
    }
}
