using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class CenitReturnOfReturn2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SourceReturnTransactionId",
                table: "ReturnOfReturnFlows",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "ReturnOfReturnFlows",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<int>(
                name: "OriginalTransactionId",
                table: "ReturnOfReturnFlows",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ParentIncomingReturnStateEventId",
                table: "ReturnOfReturnFlows",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentOutgoingReturnGeneratedId",
                table: "ReturnOfReturnFlows",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_OriginalTransactionId",
                table: "ReturnOfReturnFlows",
                column: "OriginalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_ParentIncomingReturnStateEventId",
                table: "ReturnOfReturnFlows",
                column: "ParentIncomingReturnStateEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_ParentOutgoingReturnGeneratedId",
                table: "ReturnOfReturnFlows",
                column: "ParentOutgoingReturnGeneratedId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOfReturnFlows_AchReturnsGenerated_ParentOutgoingRetur~",
                table: "ReturnOfReturnFlows",
                column: "ParentOutgoingReturnGeneratedId",
                principalTable: "AchReturnsGenerated",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOfReturnFlows_AchTransactionStateEvents_ParentIncomin~",
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
                name: "FK_ReturnOfReturnFlows_AchReturnsGenerated_ParentOutgoingRetur~",
                table: "ReturnOfReturnFlows");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOfReturnFlows_AchTransactionStateEvents_ParentIncomin~",
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
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
