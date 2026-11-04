using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

/// <summary>
/// SQL Server parity for the F3.1 PostgreSQL NACHA-origin dispatch queue.
/// Additive for existing CycleBound rows. No new dispatcher is activated in F3.2.
/// </summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261024224500_AchColFileBasedDispatchQueueF32")]
public partial class AchColFileBasedDispatchQueueF32 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Backfill at the database level as well, for existing CycleBound rows.
        migrationBuilder.AddColumn<string>(
            name: "SourceKind", table: "IncomingNachaDispatchQueue",
            type: "nvarchar(30)", maxLength: 30,
            nullable: false, defaultValue: "CycleBound");

        migrationBuilder.AddColumn<int>(
            name: "EntryDetailId", table: "IncomingNachaDispatchQueue",
            type: "int", nullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "AchTransactionId", table: "IncomingNachaDispatchQueue",
            type: "int", nullable: true,
            oldClrType: typeof(int), oldType: "int");

        migrationBuilder.AlterColumn<string>(
            name: "AchCycleId", table: "IncomingNachaDispatchQueue",
            type: "nvarchar(max)", nullable: true,
            oldClrType: typeof(string), oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "IX_IncomingNachaDispatchQueue_EntryDetailId",
            table: "IncomingNachaDispatchQueue", column: "EntryDetailId");

        migrationBuilder.AddForeignKey(
            name: "FK_IncomingNachaDispatchQueue_EntryDetails_EntryDetailId",
            table: "IncomingNachaDispatchQueue", column: "EntryDetailId",
            principalTable: "EntryDetails", principalColumn: "EntryDetailID",
            onDelete: ReferentialAction.Restrict);

        // SQL Server identifier quoting is explicit; the EF model uses an equivalent
        // provider-neutral expression with double quotes. The SQL Server constraint
        // validates pre-existing rows and enforces the same two source shapes.
        migrationBuilder.AddCheckConstraint(
            name: "CK_IncomingNachaDispatchQueue_SourceShape",
            table: "IncomingNachaDispatchQueue",
            sql: "([SourceKind] = 'CycleBound' AND [AchTransactionId] IS NOT NULL AND [AchCycleId] IS NOT NULL AND [EntryDetailId] IS NULL) OR " +
                 "([SourceKind] = 'AchColFileBased' AND [EntryDetailId] IS NOT NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Fail closed: dropping the new reference or making old columns NOT NULL
        // would lose or invalidate NACHA-origin financial queue records. This guard
        // must run before any destructive operation, in the migration transaction.
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM [IncomingNachaDispatchQueue] WITH (UPDLOCK, HOLDLOCK)
                WHERE [SourceKind] <> 'CycleBound'
                   OR [EntryDetailId] IS NOT NULL
                   OR [AchTransactionId] IS NULL
                   OR [AchCycleId] IS NULL
            )
            BEGIN
                THROW 51032, 'F3.2 rollback refused: NACHA-origin queue rows or invalid legacy rows exist; reconcile before rollback.', 1;
            END;
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
            type: "int", nullable: false,
            oldClrType: typeof(int), oldType: "int", oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "AchCycleId", table: "IncomingNachaDispatchQueue",
            type: "nvarchar(max)", nullable: false,
            oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);

        migrationBuilder.DropColumn(name: "EntryDetailId", table: "IncomingNachaDispatchQueue");
        migrationBuilder.DropColumn(name: "SourceKind", table: "IncomingNachaDispatchQueue");
    }
}
