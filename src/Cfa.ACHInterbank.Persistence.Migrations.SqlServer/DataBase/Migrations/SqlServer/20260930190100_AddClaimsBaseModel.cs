using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddClaimsBaseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClaimCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    CauseCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestingParticipantId = table.Column<int>(type: "int", nullable: false),
                    RespondingParticipantId = table.Column<int>(type: "int", nullable: false),
                    FiledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreationIdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimCases_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimCases_FinancialInstitutions_RequestingParticipantId",
                        column: x => x.RequestingParticipantId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimCases_FinancialInstitutions_RespondingParticipantId",
                        column: x => x.RespondingParticipantId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimCaseId = table.Column<int>(type: "int", nullable: false),
                    AchTransactionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimTransactions_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimTransactions_ClaimCases_ClaimCaseId",
                        column: x => x.ClaimCaseId,
                        principalTable: "ClaimCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimCases_ClearingHouseId",
                table: "ClaimCases",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimCases_ClearingHouse_Cause_FiledAt",
                table: "ClaimCases",
                columns: new[] { "ClearingHouseId", "CauseCode", "FiledAt" });

            migrationBuilder.CreateIndex(
                name: "UX_ClaimCases_CreationIdempotencyKey",
                table: "ClaimCases",
                column: "CreationIdempotencyKey",
                unique: true,
                filter: "[CreationIdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimCases_RequestingParticipantId",
                table: "ClaimCases",
                column: "RequestingParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimCases_RespondingParticipantId",
                table: "ClaimCases",
                column: "RespondingParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimTransactions_AchTransactionId",
                table: "ClaimTransactions",
                column: "AchTransactionId");

            migrationBuilder.CreateIndex(
                name: "UX_ClaimTransactions_ClaimCase_AchTransaction",
                table: "ClaimTransactions",
                columns: new[] { "ClaimCaseId", "AchTransactionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ClaimTransactions");
            migrationBuilder.DropTable(name: "ClaimCases");
        }
    }
}
