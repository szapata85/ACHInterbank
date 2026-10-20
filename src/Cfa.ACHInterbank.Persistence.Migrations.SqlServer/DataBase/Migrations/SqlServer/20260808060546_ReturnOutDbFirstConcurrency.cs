using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class ReturnOutDbFirstConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT OriginalTransactionId
                    FROM dbo.AchReturnsGenerated
                    GROUP BY OriginalTransactionId
                    HAVING COUNT_BIG(*) > 1)
                    THROW 51038, 'RETURN_OUT_MIGRATION_DUPLICATE_ORIGINAL_TRANSACTION', 1;

                IF EXISTS (
                    SELECT 1
                    FROM dbo.AchReturnsGenerated
                    WHERE LEN(NewSequenceNumber) <> 15
                       OR NewSequenceNumber LIKE '%[^0-9]%')
                    THROW 51039, 'RETURN_OUT_MIGRATION_INVALID_TRACE_NUMBER', 1;

                IF EXISTS (
                    SELECT CAST(c.ProcessingDate AS date), g.NewSequenceNumber
                    FROM dbo.AchReturnsGenerated AS g
                    INNER JOIN dbo.AchCycles AS c ON c.Id = g.ReturnCycleId
                    GROUP BY CAST(c.ProcessingDate AS date), g.NewSequenceNumber
                    HAVING COUNT_BIG(*) > 1)
                    THROW 51040, 'RETURN_OUT_MIGRATION_DUPLICATE_DAILY_TRACE_NUMBER', 1;
                """);

            migrationBuilder.DropIndex(
                name: "UX_AchReturnGenerated_OriginalTransaction_Reason_Cycle",
                table: "AchReturnsGenerated");

            migrationBuilder.AddColumn<DateOnly>(
                name: "SequenceDate",
                table: "AchReturnsGenerated",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AchReturnTraceSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantDfi = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    SequenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastAssignedValue = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchReturnTraceSequences", x => x.Id);
                    table.CheckConstraint("CK_AchReturnTraceSequence_LastAssignedValue", "\"LastAssignedValue\" >= 0 AND \"LastAssignedValue\" <= 6999999");
                });

            migrationBuilder.Sql("""
                UPDATE g
                SET SequenceDate = CAST(c.ProcessingDate AS date)
                FROM dbo.AchReturnsGenerated AS g
                INNER JOIN dbo.AchCycles AS c ON c.Id = g.ReturnCycleId;

                INSERT INTO dbo.AchReturnTraceSequences
                    (ParticipantDfi, SequenceDate, LastAssignedValue, UpdatedAtUtc)
                SELECT
                    LEFT(NewSequenceNumber, 8),
                    SequenceDate,
                    MAX(CONVERT(int, RIGHT(NewSequenceNumber, 7))),
                    SYSUTCDATETIME()
                FROM dbo.AchReturnsGenerated
                GROUP BY LEFT(NewSequenceNumber, 8), SequenceDate;
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

            migrationBuilder.CreateIndex(
                name: "UX_AchReturnGenerated_OriginalTransaction_Reason_Cycle",
                table: "AchReturnsGenerated",
                columns: new[] { "OriginalTransactionId", "ReturnReasonCode", "ReturnCycleId" },
                unique: true);
        }
    }
}
