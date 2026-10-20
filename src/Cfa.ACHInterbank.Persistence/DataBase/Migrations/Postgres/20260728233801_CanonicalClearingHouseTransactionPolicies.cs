using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    public partial class CanonicalClearingHouseTransactionPolicies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrenotificationLeadBusinessDays",
                table: "ClearingHouseTransactionRules",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "ClearingHouseTransactionRules" AS policy
                SET "PrenotificationLeadBusinessDays" = 3
                FROM "ClearingHouses" AS house
                WHERE policy."ClearingHouseId" = house."Id"
                  AND house."Code" IN ('ACHCOL', 'ACH')
                  AND policy."TransactionType" = 'Debit'
                  AND policy."PrenotificationMode" = 'Mandatory';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrenotificationLeadBusinessDays",
                table: "ClearingHouseTransactionRules");
        }
    }
}
