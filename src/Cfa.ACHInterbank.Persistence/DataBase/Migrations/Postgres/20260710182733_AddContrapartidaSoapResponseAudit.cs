using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    [DbContext(typeof(AchDbContext))]
    [Migration("20260710182733_AddContrapartidaSoapResponseAudit")]
    public partial class AddContrapartidaSoapResponseAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "ContrapartidaDispatchAttempts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionMode",
                table: "ContrapartidaDispatchAttempts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsFunctionalRejection",
                table: "ContrapartidaDispatchAttempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccessful",
                table: "ContrapartidaDispatchAttempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTechnicalFailure",
                table: "ContrapartidaDispatchAttempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SoapEndpoint",
                table: "ContrapartidaDispatchAttempts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoapMethodName",
                table: "ContrapartidaDispatchAttempts",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoapResponseCode",
                table: "ContrapartidaDispatchAttempts",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoapResponseDescription",
                table: "ContrapartidaDispatchAttempts",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoapTechnicalStatus",
                table: "ContrapartidaDispatchAttempts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TechnicalException",
                table: "ContrapartidaDispatchAttempts",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchAttempts_SoapAudit",
                table: "ContrapartidaDispatchAttempts",
                columns: new[] { "SoapMethodName", "ExecutionMode", "SoapTechnicalStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContrapartidaDispatchAttempts_SoapAudit",
                table: "ContrapartidaDispatchAttempts");

            migrationBuilder.DropColumn(name: "DurationMs", table: "ContrapartidaDispatchAttempts");
            migrationBuilder.DropColumn(name: "ExecutionMode", table: "ContrapartidaDispatchAttempts");
            migrationBuilder.DropColumn(name: "IsFunctionalRejection", table: "ContrapartidaDispatchAttempts");
            migrationBuilder.DropColumn(name: "IsSuccessful", table: "ContrapartidaDispatchAttempts");
            migrationBuilder.DropColumn(name: "IsTechnicalFailure", table: "ContrapartidaDispatchAttempts");
            migrationBuilder.DropColumn(name: "SoapEndpoint", table: "ContrapartidaDispatchAttempts");
            migrationBuilder.DropColumn(name: "SoapMethodName", table: "ContrapartidaDispatchAttempts");
            migrationBuilder.DropColumn(name: "SoapResponseCode", table: "ContrapartidaDispatchAttempts");
            migrationBuilder.DropColumn(name: "SoapResponseDescription", table: "ContrapartidaDispatchAttempts");
            migrationBuilder.DropColumn(name: "SoapTechnicalStatus", table: "ContrapartidaDispatchAttempts");
            migrationBuilder.DropColumn(name: "TechnicalException", table: "ContrapartidaDispatchAttempts");
        }
    }
}
