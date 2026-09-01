using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class OpsGap002AchColombiaManagedMft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchManagedFileTransferConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    AutomaticOutboundEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutomaticInboundEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ManualOutboundAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    ManualInboundAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    MaximumRetries = table.Column<int>(type: "integer", nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    OutboundLocation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    InboundLocation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ArchiveLocation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchManagedFileTransferConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchManagedFileTransferConfigurations_ClearingHouses_Clearin~",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchManagedFileTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LogicalFileIdentity = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    PhysicalFileName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    AchFileExportId = table.Column<int>(type: "integer", nullable: true),
                    IncomingNachaFileIngestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    RetainedContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    OperationalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AchCycleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ExecutionOrigin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TransferredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActiveStorageReference = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ArchiveReference = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ArchivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CorrectedFromTransferId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorIdentity = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RetiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetiredBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RetirementReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchManagedFileTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchManagedFileTransfers_AchCycles_AchCycleId",
                        column: x => x.AchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchManagedFileTransfers_AchFileExports_AchFileExportId",
                        column: x => x.AchFileExportId,
                        principalTable: "AchFileExports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchManagedFileTransfers_AchManagedFileTransfers_CorrectedFr~",
                        column: x => x.CorrectedFromTransferId,
                        principalTable: "AchManagedFileTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchManagedFileTransfers_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchManagedFileTransfers_IncomingNachaFileIngestions_Incomin~",
                        column: x => x.IncomingNachaFileIngestionId,
                        principalTable: "IncomingNachaFileIngestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchManagedFileTransferEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Result = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExecutionOrigin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Actor = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchManagedFileTransferEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchManagedFileTransferEvents_AchManagedFileTransfers_Transf~",
                        column: x => x.TransferId,
                        principalTable: "AchManagedFileTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchManagedFileTransferConfigurations_ClearingHouseId",
                table: "AchManagedFileTransferConfigurations",
                column: "ClearingHouseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchManagedFileTransferEvents_TransferId_OccurredAtUtc",
                table: "AchManagedFileTransferEvents",
                columns: new[] { "TransferId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AchManagedFileTransfers_AchCycleId",
                table: "AchManagedFileTransfers",
                column: "AchCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_AchManagedFileTransfers_ClearingHouseId",
                table: "AchManagedFileTransfers",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AchManagedFileTransfers_CorrectedFromTransferId",
                table: "AchManagedFileTransfers",
                column: "CorrectedFromTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_AchManagedFileTransfers_Direction_ContentSha256_FileSize",
                table: "AchManagedFileTransfers",
                columns: new[] { "Direction", "ContentSha256", "FileSize" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchManagedFileTransfers_Direction_PhysicalFileName_Operatio~",
                table: "AchManagedFileTransfers",
                columns: new[] { "Direction", "PhysicalFileName", "OperationalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AchManagedFileTransfers_IdempotencyKey",
                table: "AchManagedFileTransfers",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchManagedFileTransfers_IncomingNachaFileIngestionId",
                table: "AchManagedFileTransfers",
                column: "IncomingNachaFileIngestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AchManagedFileTransfers_Status_OperationalDate",
                table: "AchManagedFileTransfers",
                columns: new[] { "Status", "OperationalDate" });

            migrationBuilder.CreateIndex(
                name: "UX_AchManagedFileTransfers_AchFileExportId",
                table: "AchManagedFileTransfers",
                column: "AchFileExportId",
                unique: true,
                filter: "\"AchFileExportId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchManagedFileTransferConfigurations");

            migrationBuilder.DropTable(
                name: "AchManagedFileTransferEvents");

            migrationBuilder.DropTable(
                name: "AchManagedFileTransfers");
        }
    }
}
