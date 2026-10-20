using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class SyncSqlServerModel20261020 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IntegrationMappingSets_ContractVersionId",
                table: "IntegrationMappingSets");

            migrationBuilder.AlterColumn<string>(
                name: "PrenotificationResponseDeadlineMode",
                table: "ClearingHouseTransactionRules",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "NotApplicable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PrenotificationResponseDeadlineMode",
                table: "ClearingHouseTransactionRules",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NotApplicable",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingSets_ContractVersionId",
                table: "IntegrationMappingSets",
                column: "ContractVersionId");
        }
    }
}
