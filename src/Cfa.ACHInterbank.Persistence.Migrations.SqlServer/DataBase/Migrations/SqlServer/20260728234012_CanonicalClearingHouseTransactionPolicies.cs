using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    public partial class CanonicalClearingHouseTransactionPolicies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrenotificationLeadBusinessDays",
                table: "ClearingHouseTransactionRules",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE policy
                SET policy.[PrenotificationLeadBusinessDays] = 3
                FROM [ClearingHouseTransactionRules] AS policy
                INNER JOIN [ClearingHouses] AS house ON policy.[ClearingHouseId] = house.[Id]
                WHERE house.[Code] IN ('ACHCOL', 'ACH')
                  AND policy.[TransactionType] = 'Debit'
                  AND policy.[PrenotificationMode] = 'Mandatory';
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
