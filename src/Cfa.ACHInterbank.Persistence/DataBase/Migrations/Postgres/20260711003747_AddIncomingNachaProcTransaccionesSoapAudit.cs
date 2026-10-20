using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddIncomingNachaProcTransaccionesSoapAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "IncomingNachaIntegrationExecution",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionMode",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<bool>(
                name: "IsFunctionalRejection",
                table: "IncomingNachaIntegrationExecution",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccessful",
                table: "IncomingNachaIntegrationExecution",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTechnicalFailure",
                table: "IncomingNachaIntegrationExecution",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SoapEndpoint",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoapMethodName",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Proc_Transacciones");

            migrationBuilder.AddColumn<string>(
                name: "SoapResponseCode",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoapResponseDescription",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoapTechnicalStatus",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "TechnicalException",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "IncomingNachaIntegrationExecution"
                SET
                    "SoapMethodName" = CASE WHEN "MethodName" IS NULL OR btrim("MethodName") = '' THEN "SoapMethodName" ELSE "MethodName" END,
                    "SoapResponseCode" = CASE WHEN "ResponseCode" IS NULL OR btrim("ResponseCode") = '' THEN "SoapResponseCode" ELSE "ResponseCode" END,
                    "SoapResponseDescription" = CASE WHEN "ResponseMessage" IS NULL OR btrim("ResponseMessage") = '' THEN "SoapResponseDescription" ELSE "ResponseMessage" END,
                    "IsSuccessful" = "IsSuccess",
                    "SoapTechnicalStatus" = CASE
                        WHEN "IsSuccess" = TRUE THEN 'Succeeded'
                        WHEN "IsRetryable" = TRUE THEN 'RetryableFailure'
                        ELSE 'UnknownFailure'
                    END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_DispatchQueueId",
                table: "IncomingNachaIntegrationExecution",
                column: "DispatchQueueId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_SoapMethodName",
                table: "IncomingNachaIntegrationExecution",
                column: "SoapMethodName");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_SoapResponseCode",
                table: "IncomingNachaIntegrationExecution",
                column: "SoapResponseCode");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_SoapTechnicalStatus",
                table: "IncomingNachaIntegrationExecution",
                column: "SoapTechnicalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_StartedAtUtc",
                table: "IncomingNachaIntegrationExecution",
                column: "StartedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaIntegrationExecution_DispatchQueueId",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaIntegrationExecution_SoapMethodName",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaIntegrationExecution_SoapResponseCode",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaIntegrationExecution_SoapTechnicalStatus",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaIntegrationExecution_StartedAtUtc",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "ExecutionMode",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "IsFunctionalRejection",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "IsSuccessful",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "IsTechnicalFailure",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "SoapEndpoint",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "SoapMethodName",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "SoapResponseCode",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "SoapResponseDescription",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "SoapTechnicalStatus",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "TechnicalException",
                table: "IncomingNachaIntegrationExecution");
        }
    }
}
