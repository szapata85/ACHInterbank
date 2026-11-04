using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

[DbContext(typeof(AchDbContext))]
[Migration("20261024235500_AchColFileBasedQueuePlannerF33")]
public partial class AchColFileBasedQueuePlannerF33 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_IncomingNachaDispatchQueue_SourceShape", "IncomingNachaDispatchQueue");
        migrationBuilder.AlterColumn<Guid>(
            name: "IncomingNachaTransactionLinkId", table: "IncomingNachaDispatchQueue",
            type: "uuid", nullable: true,
            oldClrType: typeof(Guid), oldType: "uuid");
        migrationBuilder.AddCheckConstraint(
            name: "CK_IncomingNachaDispatchQueue_SourceShape", table: "IncomingNachaDispatchQueue",
            sql: "(\"SourceKind\" = 'CycleBound' AND \"AchTransactionId\" IS NOT NULL AND \"AchCycleId\" IS NOT NULL AND \"EntryDetailId\" IS NULL AND \"IncomingNachaTransactionLinkId\" IS NOT NULL) OR " +
                 "(\"SourceKind\" = 'AchColFileBased' AND \"EntryDetailId\" IS NOT NULL AND \"AchTransactionId\" IS NULL AND \"AchCycleId\" IS NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Do not silently drop NACHA dispatch records or fabricate transaction links.
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM "IncomingNachaDispatchQueue"
                    WHERE "IncomingNachaTransactionLinkId" IS NULL) THEN
                    RAISE EXCEPTION 'F3.3 downgrade refused: file-based entries must be reconciled before schema rollback.';
                END IF;
            END $$;
            """);
        migrationBuilder.DropCheckConstraint("CK_IncomingNachaDispatchQueue_SourceShape", "IncomingNachaDispatchQueue");
        migrationBuilder.AlterColumn<Guid>(
            name: "IncomingNachaTransactionLinkId", table: "IncomingNachaDispatchQueue",
            type: "uuid", nullable: false,
            oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AddCheckConstraint(
            name: "CK_IncomingNachaDispatchQueue_SourceShape", table: "IncomingNachaDispatchQueue",
            sql: "(\"SourceKind\" = 'CycleBound' AND \"AchTransactionId\" IS NOT NULL AND \"AchCycleId\" IS NOT NULL AND \"EntryDetailId\" IS NULL) OR " +
                 "(\"SourceKind\" = 'AchColFileBased' AND \"EntryDetailId\" IS NOT NULL)");
    }
}
