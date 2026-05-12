using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RemoveSeedCycleFromAchCycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "AchCycles" c
                WHERE c."Id" = 'SEED-CYCLE'
                  AND NOT EXISTS (SELECT 1 FROM "AchBatches" b WHERE b."AchCycleId" = c."Id")
                  AND NOT EXISTS (SELECT 1 FROM "AchTransactions" t WHERE t."AchCycleId" = c."Id")
                  AND NOT EXISTS (SELECT 1 FROM "AchFileExports" f WHERE f."AchCycleId" = c."Id")
                  AND NOT EXISTS (SELECT 1 FROM "NachaHeaders" h WHERE h."AchCycleId" = c."Id")
                  AND NOT EXISTS (SELECT 1 FROM "CenitCycleExecutions" e WHERE e."AchCycleId" = c."Id")
                  AND NOT EXISTS (SELECT 1 FROM "CenitCycleQueue" q WHERE q."TargetAchCycleId" = c."Id")
                  AND NOT EXISTS (SELECT 1 FROM "AchReturnGenerated" r WHERE r."ReturnCycleId" = c."Id")
                  AND NOT EXISTS (SELECT 1 FROM "ContrapartidaDispatchBatches" cb WHERE cb."AchCycleId" = c."Id")
                  AND NOT EXISTS (SELECT 1 FROM "ContrapartidaDispatchItems" ci WHERE ci."AchCycleId" = c."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
