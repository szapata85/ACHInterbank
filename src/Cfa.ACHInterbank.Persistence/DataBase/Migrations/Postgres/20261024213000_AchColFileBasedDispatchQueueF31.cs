using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

/// <summary>
/// Expand-only queue model for NACHA-origin dispatch. F3.1 does not enqueue/directly dispatch
/// NACHA entries yet; SQL Server equivalent is delivered in F3.2.
/// </summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261024213000_AchColFileBasedDispatchQueueF31")]
public partial class AchColFileBasedDispatchQueueF31 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SourceKind", table: "IncomingNachaDispatchQueue",
            type: "character varying(30)", maxLength: 30,
            nullable: false, defaultValue: "CycleBound");

        migrationBuilder.AddColumn<int>(
            name: "EntryDetailId", table: "IncomingNachaDispatchQueue",
            type: "integer", nullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "AchTransactionId", table: "IncomingNachaDispatchQueue",
            type: "integer", nullable: true,
            oldClrType: typeof(int), oldType: "integer");

        migrationBuilder.AlterColumn<string>(
            name: "AchCycleId", table: "IncomingNachaDispatchQueue",
            type: "text", nullable: true,
            oldClrType: typeof(string), oldType: "text");

        migrationBuilder.CreateIndex(
            name: "IX_IncomingNachaDispatchQueue_EntryDetailId",
            table: "IncomingNachaDispatchQueue", column: "EntryDetailId");

        migrationBuilder.AddForeignKey(
            name: "FK_IncomingNachaDispatchQueue_EntryDetails_EntryDetailId",
            table: "IncomingNachaDispatchQueue", column: "EntryDetailId",
            principalTable: "EntryDetails", principalColumn: "EntryDetailID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddCheckConstraint(
            name: "CK_IncomingNachaDispatchQueue_SourceShape",
            table: "IncomingNachaDispatchQueue",
            sql: "(\"SourceKind\" = 'CycleBound' AND \"AchTransactionId\" IS NOT NULL AND \"AchCycleId\" IS NOT NULL AND \"EntryDetailId\" IS NULL) OR " +
                 "(\"SourceKind\" = 'AchColFileBased' AND \"EntryDetailId\" IS NOT NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Downgrade must never truncate or hide queued monetary requests. Fail closed.
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM "IncomingNachaDispatchQueue"
                    WHERE "SourceKind" <> 'CycleBound' OR "EntryDetailId" IS NOT NULL
                        OR "AchTransactionId" IS NULL OR "AchCycleId" IS NULL
                ) THEN
                    RAISE EXCEPTION 'F3.1 downgrade refused: NACHA-origin queue rows or invalid legacy rows exist; reconcile before rollback.';
                END IF;
            END $$;
            """);

        migrationBuilder.DropCheckConstraint(
            name: "CK_IncomingNachaDispatchQueue_SourceShape",
            table: "IncomingNachaDispatchQueue");

        migrationBuilder.DropForeignKey(
            name: "FK_IncomingNachaDispatchQueue_EntryDetails_EntryDetailId",
            table: "IncomingNachaDispatchQueue");

        migrationBuilder.DropIndex(
            name: "IX_IncomingNachaDispatchQueue_EntryDetailId",
            table: "IncomingNachaDispatchQueue");

        migrationBuilder.AlterColumn<int>(
            name: "AchTransactionId", table: "IncomingNachaDispatchQueue",
            type: "integer", nullable: false,
            oldClrType: typeof(int), oldType: "integer", oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "AchCycleId", table: "IncomingNachaDispatchQueue",
            type: "text", nullable: false,
            oldClrType: typeof(string), oldType: "text", oldNullable: true);

        migrationBuilder.DropColumn(name: "EntryDetailId", table: "IncomingNachaDispatchQueue");
        migrationBuilder.DropColumn(name: "SourceKind", table: "IncomingNachaDispatchQueue");
    }
}
