using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AtomicTransactionTraceAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AchTransactions_TraceNumber",
                table: "AchTransactions");

            migrationBuilder.CreateTable(
                name: "AchTransactionTraceSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginatingDfi = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    SequenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastAssignedValue = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchTransactionTraceSequences", x => x.Id);
                    table.CheckConstraint("CK_AchTransactionTraceSequence_LastAssignedValue", "[LastAssignedValue] >= 0 AND [LastAssignedValue] <= 6999999");
                });

            migrationBuilder.Sql(
                """
                INSERT INTO dbo.AchTransactionTraceSequences
                    (OriginatingDfi, SequenceDate, LastAssignedValue, UpdatedAtUtc)
                SELECT SUBSTRING(TraceNumber, 1, 8),
                       CAST(EffectiveEntryDate AS date),
                       MAX(TraceSequenceNumber),
                       SYSUTCDATETIME()
                FROM dbo.AchTransactions
                WHERE LEN(TraceNumber) >= 15
                GROUP BY SUBSTRING(TraceNumber, 1, 8), CAST(EffectiveEntryDate AS date);
                """);

            migrationBuilder.CreateIndex(
                name: "UX_AchTransactions_EffectiveEntryDate_TraceNumber",
                table: "AchTransactions",
                columns: new[] { "EffectiveEntryDate", "TraceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AchTransactionTraceSequence_Dfi_Date",
                table: "AchTransactionTraceSequences",
                columns: new[] { "OriginatingDfi", "SequenceDate" },
                unique: true);

            migrationBuilder.Sql("""
                CREATE TRIGGER dbo.TR_AchTransactions_SyncTraceSequence
                ON dbo.AchTransactions
                AFTER INSERT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    MERGE dbo.AchTransactionTraceSequences WITH (HOLDLOCK) AS target
                    USING (
                        SELECT
                            LEFT(TraceNumber, 8) AS OriginatingDfi,
                            CAST(EffectiveEntryDate AS date) AS SequenceDate,
                            MAX(TraceSequenceNumber) AS LastAssignedValue
                        FROM inserted
                        WHERE TraceSequenceNumber BETWEEN 1 AND 6999999
                          AND LEN(TraceNumber) >= 8
                        GROUP BY LEFT(TraceNumber, 8), CAST(EffectiveEntryDate AS date)
                    ) AS source
                    ON target.OriginatingDfi = source.OriginatingDfi
                       AND target.SequenceDate = source.SequenceDate
                    WHEN MATCHED AND source.LastAssignedValue > target.LastAssignedValue THEN
                        UPDATE SET
                            LastAssignedValue = source.LastAssignedValue,
                            UpdatedAtUtc = SYSUTCDATETIME()
                    WHEN NOT MATCHED THEN
                        INSERT (OriginatingDfi, SequenceDate, LastAssignedValue, UpdatedAtUtc)
                        VALUES (source.OriginatingDfi, source.SequenceDate, source.LastAssignedValue, SYSUTCDATETIME());
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TR_AchTransactions_SyncTraceSequence;");

            migrationBuilder.DropTable(
                name: "AchTransactionTraceSequences");

            migrationBuilder.DropIndex(
                name: "UX_AchTransactions_EffectiveEntryDate_TraceNumber",
                table: "AchTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_TraceNumber",
                table: "AchTransactions",
                column: "TraceNumber");
        }
    }
}
