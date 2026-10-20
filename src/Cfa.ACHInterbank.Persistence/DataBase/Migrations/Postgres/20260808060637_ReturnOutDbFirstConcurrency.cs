using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class ReturnOutDbFirstConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT "OriginalTransactionId"
                        FROM "AchReturnsGenerated"
                        GROUP BY "OriginalTransactionId"
                        HAVING COUNT(*) > 1) THEN
                        RAISE EXCEPTION 'RETURN_OUT_MIGRATION_DUPLICATE_ORIGINAL_TRANSACTION';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "AchReturnsGenerated"
                        WHERE "NewSequenceNumber" !~ '^[0-9]{15}$') THEN
                        RAISE EXCEPTION 'RETURN_OUT_MIGRATION_INVALID_TRACE_NUMBER';
                    END IF;

                    IF EXISTS (
                        SELECT c."ProcessingDate"::date, g."NewSequenceNumber"
                        FROM "AchReturnsGenerated" AS g
                        INNER JOIN "AchCycles" AS c ON c."Id" = g."ReturnCycleId"
                        GROUP BY c."ProcessingDate"::date, g."NewSequenceNumber"
                        HAVING COUNT(*) > 1) THEN
                        RAISE EXCEPTION 'RETURN_OUT_MIGRATION_DUPLICATE_DAILY_TRACE_NUMBER';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "UX_AchReturnGenerated_OriginalTransaction_Reason_Cycle",
                table: "AchReturnsGenerated");

            migrationBuilder.AlterColumn<string>(
                name: "BeforeJson",
                table: "HistConfigChange",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(16000)",
                oldMaxLength: 16000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AfterJson",
                table: "HistConfigChange",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(16000)",
                oldMaxLength: 16000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SequenceDate",
                table: "AchReturnsGenerated",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AchReturnTraceSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipantDfi = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    SequenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastAssignedValue = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchReturnTraceSequences", x => x.Id);
                    table.CheckConstraint("CK_AchReturnTraceSequence_LastAssignedValue", "\"LastAssignedValue\" >= 0 AND \"LastAssignedValue\" <= 6999999");
                });

            migrationBuilder.Sql("""
                UPDATE "AchReturnsGenerated" AS g
                SET "SequenceDate" = c."ProcessingDate"::date
                FROM "AchCycles" AS c
                WHERE c."Id" = g."ReturnCycleId";

                INSERT INTO "AchReturnTraceSequences"
                    ("ParticipantDfi", "SequenceDate", "LastAssignedValue", "UpdatedAtUtc")
                SELECT
                    LEFT("NewSequenceNumber", 8),
                    "SequenceDate",
                    MAX(RIGHT("NewSequenceNumber", 7)::integer),
                    CURRENT_TIMESTAMP
                FROM "AchReturnsGenerated"
                GROUP BY LEFT("NewSequenceNumber", 8), "SequenceDate";
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "SequenceDate",
                table: "AchReturnsGenerated",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_AchReturnGenerated_OriginalTransaction",
                table: "AchReturnsGenerated",
                column: "OriginalTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AchReturnGenerated_SequenceDate_Trace",
                table: "AchReturnsGenerated",
                columns: new[] { "SequenceDate", "NewSequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AchReturnTraceSequence_Participant_Date",
                table: "AchReturnTraceSequences",
                columns: new[] { "ParticipantDfi", "SequenceDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchReturnTraceSequences");

            migrationBuilder.DropIndex(
                name: "UX_AchReturnGenerated_OriginalTransaction",
                table: "AchReturnsGenerated");

            migrationBuilder.DropIndex(
                name: "UX_AchReturnGenerated_SequenceDate_Trace",
                table: "AchReturnsGenerated");

            migrationBuilder.DropColumn(
                name: "SequenceDate",
                table: "AchReturnsGenerated");

            migrationBuilder.AlterColumn<string>(
                name: "BeforeJson",
                table: "HistConfigChange",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AfterJson",
                table: "HistConfigChange",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_AchReturnGenerated_OriginalTransaction_Reason_Cycle",
                table: "AchReturnsGenerated",
                columns: new[] { "OriginalTransactionId", "ReturnReasonCode", "ReturnCycleId" },
                unique: true);
        }
    }
}
