using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

/// <summary>Reserva única de candidatas ACH Colombia; no genera retorno financiero.</summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261102230000_AchColombiaReturnPreparationI03")]
public partial class AchColombiaReturnPreparationI03 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AchColombiaReturnPreparations",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OriginalTransactionId = table.Column<int>(type: "int", nullable: false),
                SourceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OriginalCycleId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                SuggestedReturnCycleId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                ReasonCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                PreparedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AchColombiaReturnPreparations", x => x.Id);
                table.ForeignKey(
                    name: "FK_AchColombiaReturnPreparations_AchTransactions_OriginalTransactionId",
                    column: x => x.OriginalTransactionId,
                    principalTable: "AchTransactions", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AchColombiaReturnPreparations_IncomingNachaProcessingEvents_SourceEventId",
                    column: x => x.SourceEventId,
                    principalTable: "IncomingNachaProcessingEvents", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "UX_AchColReturnPreparation_Original",
            table: "AchColombiaReturnPreparations", column: "OriginalTransactionId", unique: true);
        migrationBuilder.CreateIndex(name: "UX_AchColReturnPreparation_Event",
            table: "AchColombiaReturnPreparations", column: "SourceEventId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "AchColombiaReturnPreparations");
}
