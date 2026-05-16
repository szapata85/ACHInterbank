using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    public partial class AddUniqueIndexForAchReturnGeneratedIdempotency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AchReturnsGenerated_OriginalTransactionId_ReturnReasonCode_ReturnCycleId",
                table: "AchReturnsGenerated");

            migrationBuilder.CreateIndex(
                name: "UX_AchReturnGenerated_OriginalTransaction_Reason_Cycle",
                table: "AchReturnsGenerated",
                columns: new[] { "OriginalTransactionId", "ReturnReasonCode", "ReturnCycleId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AchReturnGenerated_OriginalTransaction_Reason_Cycle",
                table: "AchReturnsGenerated");

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnsGenerated_OriginalTransactionId_ReturnReasonCode_ReturnCycleId",
                table: "AchReturnsGenerated",
                columns: new[] { "OriginalTransactionId", "ReturnReasonCode", "ReturnCycleId" });
        }
    }
}
