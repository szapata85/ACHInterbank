using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

[DbContext(typeof(AchDbContext))]
[Migration("20261024235500_AchColFileBasedQueuePlannerF33")]
public partial class AchColFileBasedQueuePlannerF33 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_IncomingNachaDispatchQueue_SourceShape", "IncomingNachaDispatchQueue");
        migrationBuilder.AlterColumn<Guid>(
            name: "IncomingNachaTransactionLinkId", table: "IncomingNachaDispatchQueue",
            type: "uniqueidentifier", nullable: true,
            oldClrType: typeof(Guid), oldType: "uniqueidentifier");
        migrationBuilder.AddCheckConstraint(
            name: "CK_IncomingNachaDispatchQueue_SourceShape", table: "IncomingNachaDispatchQueue",
            sql: "([SourceKind] = 'CycleBound' AND [AchTransactionId] IS NOT NULL AND [AchCycleId] IS NOT NULL AND [EntryDetailId] IS NULL AND [IncomingNachaTransactionLinkId] IS NOT NULL) OR " +
                 "([SourceKind] = 'AchColFileBased' AND [EntryDetailId] IS NOT NULL AND [AchTransactionId] IS NULL AND [AchCycleId] IS NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM [IncomingNachaDispatchQueue] WITH (UPDLOCK, HOLDLOCK)
                WHERE [IncomingNachaTransactionLinkId] IS NULL)
            BEGIN
                THROW 51033, 'F3.3 rollback refused: file-based queue entries require reconciliation.', 1;
            END;
            """);
        migrationBuilder.DropCheckConstraint("CK_IncomingNachaDispatchQueue_SourceShape", "IncomingNachaDispatchQueue");
        migrationBuilder.AlterColumn<Guid>(
            name: "IncomingNachaTransactionLinkId", table: "IncomingNachaDispatchQueue",
            type: "uniqueidentifier", nullable: false,
            oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);
        migrationBuilder.AddCheckConstraint(
            name: "CK_IncomingNachaDispatchQueue_SourceShape", table: "IncomingNachaDispatchQueue",
            sql: "([SourceKind] = 'CycleBound' AND [AchTransactionId] IS NOT NULL AND [AchCycleId] IS NOT NULL AND [EntryDetailId] IS NULL) OR " +
                 "([SourceKind] = 'AchColFileBased' AND [EntryDetailId] IS NOT NULL)");
    }
}
