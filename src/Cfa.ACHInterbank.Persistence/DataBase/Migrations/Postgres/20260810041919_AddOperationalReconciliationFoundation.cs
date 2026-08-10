using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddOperationalReconciliationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchOperationalReconciliationSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    OperationalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AchCycleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SentCount = table.Column<int>(type: "integer", nullable: false),
                    SentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceivedCount = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AppliedCount = table.Column<int>(type: "integer", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ParticipantReturnCount = table.Column<int>(type: "integer", nullable: false),
                    ParticipantReturnAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OperatorReturnCount = table.Column<int>(type: "integer", nullable: false),
                    OperatorReturnAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InternalExpectedNetPosition = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExternalEvidenceReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalSentCount = table.Column<int>(type: "integer", nullable: true),
                    ExternalSentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExternalReceivedCount = table.Column<int>(type: "integer", nullable: true),
                    ExternalReceivedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExternalNetPosition = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExternalEvidenceRecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CalculatedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchOperationalReconciliationSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchOperationalReconciliationSnapshots_AchCycles_AchCycleId",
                        column: x => x.AchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchOperationalReconciliationSnapshots_ClearingHouses_Cleari~",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchOperationalReconciliationDifferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    InternalValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExternalValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Delta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    EvidenceSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchOperationalReconciliationDifferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchOperationalReconciliationDifferences_AchOperationalRecon~",
                        column: x => x.SnapshotId,
                        principalTable: "AchOperationalReconciliationSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchOperationalReconciliationDifferences_SnapshotId_Category",
                table: "AchOperationalReconciliationDifferences",
                columns: new[] { "SnapshotId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchOperationalReconciliationSnapshots_AchCycleId",
                table: "AchOperationalReconciliationSnapshots",
                column: "AchCycleId");

            migrationBuilder.CreateIndex(
                name: "UX_AchOperationalReconciliation_Identity_Fingerprint",
                table: "AchOperationalReconciliationSnapshots",
                columns: new[] { "ClearingHouseId", "OperationalDate", "AchCycleId", "SourceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AchOperationalReconciliation_Identity_Revision",
                table: "AchOperationalReconciliationSnapshots",
                columns: new[] { "ClearingHouseId", "OperationalDate", "AchCycleId", "Revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchOperationalReconciliationDifferences");

            migrationBuilder.DropTable(
                name: "AchOperationalReconciliationSnapshots");
        }
    }
}
