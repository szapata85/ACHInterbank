using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddOutboundReturnTransportLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchFileTransmissionAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AchFileExportId = table.Column<int>(type: "integer", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Retryable = table.Column<bool>(type: "boolean", nullable: false),
                    ResultCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ResultSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ContentSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedContent = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchFileTransmissionAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchFileTransmissionAttempts_AchFileExports_AchFileExportId",
                        column: x => x.AchFileExportId,
                        principalTable: "AchFileExports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchFileTransportResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AchFileExportId = table.Column<int>(type: "integer", nullable: true),
                    ExternalEventId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FunctionalIdentityHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    TransmissionReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResultCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ResultSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrelationStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Applied = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresManualReview = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchFileTransportResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchFileTransportResults_AchFileExports_AchFileExportId",
                        column: x => x.AchFileExportId,
                        principalTable: "AchFileExports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchFileTransmissionAttempts_AchFileExportId_AttemptNumber",
                table: "AchFileTransmissionAttempts",
                columns: new[] { "AchFileExportId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchFileTransmissionAttempts_IdempotencyKey",
                table: "AchFileTransmissionAttempts",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchFileTransportResults_AchFileExportId",
                table: "AchFileTransportResults",
                column: "AchFileExportId");

            migrationBuilder.CreateIndex(
                name: "IX_AchFileTransportResults_ExternalEventId",
                table: "AchFileTransportResults",
                column: "ExternalEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchFileTransportResults_FunctionalIdentityHash",
                table: "AchFileTransportResults",
                column: "FunctionalIdentityHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchFileTransportResults_TransmissionReference_FileName",
                table: "AchFileTransportResults",
                columns: new[] { "TransmissionReference", "FileName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchFileTransmissionAttempts");

            migrationBuilder.DropTable(
                name: "AchFileTransportResults");
        }
    }
}
