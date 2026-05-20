using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddUatMaturedDebitAddendaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "AchTransactionAddenda" a
                SET "CollectorId" = '9001234567',
                    "ReceiverCustomerCode" = CASE
                        WHEN t."Reference" = 'UAT-ACH-DEB-MATURED-001' THEN 'CLI0000000001'
                        WHEN t."Reference" = 'UAT-CEN-DEB-MATURED-001' THEN 'CLI0000000002'
                        ELSE a."ReceiverCustomerCode"
                    END,
                    "ServiceDescription" = 'FACTURA',
                    "UpdatedAt" = timezone('utc', now())
                FROM "AchTransactions" t
                WHERE a."AchTransactionId" = t."Id"
                  AND t."Reference" IN ('UAT-ACH-DEB-MATURED-001','UAT-CEN-DEB-MATURED-001');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "AchTransactionAddenda" a
                SET "CollectorId" = NULL,
                    "ReceiverCustomerCode" = NULL,
                    "ServiceDescription" = NULL,
                    "UpdatedAt" = timezone('utc', now())
                FROM "AchTransactions" t
                WHERE a."AchTransactionId" = t."Id"
                  AND t."Reference" IN ('UAT-ACH-DEB-MATURED-001','UAT-CEN-DEB-MATURED-001');
                """);
        }
    }
}
