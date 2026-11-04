using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

/// <summary>I03-CENIT: reservas únicas sin generación ni envío.</summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261103081000_CenitReturnPreparationI03")]
public partial class CenitReturnPreparationI03 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "CenitReturnPreparations", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            OriginalTransactionId = table.Column<int>(type: "integer", nullable: false),
            SourceExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
            SourceQueueId = table.Column<Guid>(type: "uuid", nullable: false),
            OriginalCycleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            SuggestedReturnCycleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            ReasonCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            OriginalValueDate = table.Column<DateTime>(type: "date", nullable: false),
            SuggestedValueDate = table.Column<DateTime>(type: "date", nullable: false),
            PreparedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_CenitReturnPreparations", x => x.Id);
            table.ForeignKey("FK_CenitReturnPreparations_AchTransactions_OriginalTransactionId",
                x => x.OriginalTransactionId, "AchTransactions", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_CenitReturnPreparations_IncomingNachaIntegrationExecution_SourceExecutionId",
                x => x.SourceExecutionId, "IncomingNachaIntegrationExecution", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("UX_CenitReturnPreparation_Original", "CenitReturnPreparations",
            "OriginalTransactionId", unique: true);
        migrationBuilder.CreateIndex("UX_CenitReturnPreparation_Execution", "CenitReturnPreparations",
            "SourceExecutionId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "CenitReturnPreparations");
}
