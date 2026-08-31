using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class OpsGap004CenitChamberResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChamberResponseState",
                table: "AchFileExports",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "ChamberResponseUpdatedAtUtc",
                table: "AchFileExports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CenitChamberResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    AchFileExportId = table.Column<int>(type: "integer", nullable: true),
                    AchTransactionId = table.Column<int>(type: "integer", nullable: true),
                    SourceResponseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    ResponseType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResultingState = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrelationOutcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RawTechnicalReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RelatedOutboundFileName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    RelatedReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TransactionTraceNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ProblemCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    IsApplied = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CenitChamberResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CenitChamberResponses_AchFileExports_AchFileExportId",
                        column: x => x.AchFileExportId,
                        principalTable: "AchFileExports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenitChamberResponses_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenitChamberResponses_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CenitChamberResponses_AchFileExportId_ReceivedAtUtc",
                table: "CenitChamberResponses",
                columns: new[] { "AchFileExportId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CenitChamberResponses_AchTransactionId",
                table: "CenitChamberResponses",
                column: "AchTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CenitChamberResponses_ClearingHouseId_SourceResponseId",
                table: "CenitChamberResponses",
                columns: new[] { "ClearingHouseId", "SourceResponseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CenitChamberResponses_CorrelationOutcome_ReceivedAtUtc",
                table: "CenitChamberResponses",
                columns: new[] { "CorrelationOutcome", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CenitChamberResponses_IdempotencyKey",
                table: "CenitChamberResponses",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CenitChamberResponses");

            migrationBuilder.DropColumn(
                name: "ChamberResponseState",
                table: "AchFileExports");

            migrationBuilder.DropColumn(
                name: "ChamberResponseUpdatedAtUtc",
                table: "AchFileExports");
        }
    }
}
