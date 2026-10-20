using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OriginatingDfi = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    SequenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastAssignedValue = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchTransactionTraceSequences", x => x.Id);
                    table.CheckConstraint("CK_AchTransactionTraceSequence_LastAssignedValue", "\"LastAssignedValue\" >= 0 AND \"LastAssignedValue\" <= 6999999");
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "AchTransactionTraceSequences"
                    ("OriginatingDfi", "SequenceDate", "LastAssignedValue", "UpdatedAtUtc")
                SELECT SUBSTRING("TraceNumber", 1, 8),
                       "EffectiveEntryDate"::date,
                       MAX("TraceSequenceNumber"),
                       NOW()
                FROM "AchTransactions"
                WHERE LENGTH("TraceNumber") >= 15
                GROUP BY SUBSTRING("TraceNumber", 1, 8), "EffectiveEntryDate"::date;
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
                CREATE FUNCTION sync_ach_transaction_trace_sequence()
                RETURNS trigger AS $$
                BEGIN
                    IF NEW."TraceSequenceNumber" BETWEEN 1 AND 6999999
                       AND length(NEW."TraceNumber") >= 8 THEN
                        INSERT INTO "AchTransactionTraceSequences"
                            ("OriginatingDfi", "SequenceDate", "LastAssignedValue", "UpdatedAtUtc")
                        VALUES
                            (substring(NEW."TraceNumber" from 1 for 8), CAST(NEW."EffectiveEntryDate" AS date), NEW."TraceSequenceNumber", timezone('utc', now()))
                        ON CONFLICT ("OriginatingDfi", "SequenceDate")
                        DO UPDATE SET
                            "LastAssignedValue" = GREATEST("AchTransactionTraceSequences"."LastAssignedValue", EXCLUDED."LastAssignedValue"),
                            "UpdatedAtUtc" = CASE
                                WHEN EXCLUDED."LastAssignedValue" > "AchTransactionTraceSequences"."LastAssignedValue"
                                THEN EXCLUDED."UpdatedAtUtc"
                                ELSE "AchTransactionTraceSequences"."UpdatedAtUtc"
                            END;
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER "TR_AchTransactions_SyncTraceSequence"
                AFTER INSERT ON "AchTransactions"
                FOR EACH ROW
                EXECUTE FUNCTION sync_ach_transaction_trace_sequence();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_AchTransactions_SyncTraceSequence" ON "AchTransactions";
                DROP FUNCTION IF EXISTS sync_ach_transaction_trace_sequence();
                """);

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
