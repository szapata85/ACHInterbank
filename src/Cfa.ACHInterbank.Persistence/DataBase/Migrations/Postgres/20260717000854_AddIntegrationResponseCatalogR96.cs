using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddIntegrationResponseCatalogR96 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessStatus",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAtUtc",
                table: "IncomingNachaIntegrationExecution",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresManualReview",
                table: "IncomingNachaIntegrationExecution",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ResponseCatalogId",
                table: "IncomingNachaIntegrationExecution",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RetryAllowed",
                table: "IncomingNachaIntegrationExecution",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TransportStatus",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NotExecuted");

            migrationBuilder.AddColumn<string>(
                name: "BusinessStatus",
                table: "ContrapartidaDispatchAttempts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAtUtc",
                table: "ContrapartidaDispatchAttempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresManualReview",
                table: "ContrapartidaDispatchAttempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ResponseCatalogId",
                table: "ContrapartidaDispatchAttempts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RetryAllowed",
                table: "ContrapartidaDispatchAttempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TransportStatus",
                table: "ContrapartidaDispatchAttempts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NotExecuted");

            migrationBuilder.CreateTable(
                name: "IntegrationResponseCodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MethodId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BusinessStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RetryAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresManualReview = table.Column<bool>(type: "boolean", nullable: false),
                    TargetTransactionState = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationResponseCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationResponseCodes_IntegrationMethods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "IntegrationMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_BusinessStatus_ProcessedA~",
                table: "IncomingNachaIntegrationExecution",
                columns: new[] { "BusinessStatus", "ProcessedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_ResponseCatalogId",
                table: "IncomingNachaIntegrationExecution",
                column: "ResponseCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchAttempts_BusinessStatus_ProcessedAtUtc",
                table: "ContrapartidaDispatchAttempts",
                columns: new[] { "BusinessStatus", "ProcessedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchAttempts_ResponseCatalogId",
                table: "ContrapartidaDispatchAttempts",
                column: "ResponseCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationResponseCodes_Category_IsActive",
                table: "IntegrationResponseCodes",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationResponseCodes_EffectiveFromUtc_EffectiveToUtc",
                table: "IntegrationResponseCodes",
                columns: new[] { "EffectiveFromUtc", "EffectiveToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationResponseCodes_MethodId",
                table: "IntegrationResponseCodes",
                column: "MethodId");

            migrationBuilder.CreateIndex(
                name: "UX_IntegrationResponseCodes_Source_Method_Code",
                table: "IntegrationResponseCodes",
                columns: new[] { "Source", "MethodId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContrapartidaDispatchAttempts_IntegrationResponseCodes_Resp~",
                table: "ContrapartidaDispatchAttempts",
                column: "ResponseCatalogId",
                principalTable: "IntegrationResponseCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingNachaIntegrationExecution_IntegrationResponseCodes_~",
                table: "IncomingNachaIntegrationExecution",
                column: "ResponseCatalogId",
                principalTable: "IntegrationResponseCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContrapartidaDispatchAttempts_IntegrationResponseCodes_Resp~",
                table: "ContrapartidaDispatchAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_IncomingNachaIntegrationExecution_IntegrationResponseCodes_~",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropTable(
                name: "IntegrationResponseCodes");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaIntegrationExecution_BusinessStatus_ProcessedA~",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaIntegrationExecution_ResponseCatalogId",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropIndex(
                name: "IX_ContrapartidaDispatchAttempts_BusinessStatus_ProcessedAtUtc",
                table: "ContrapartidaDispatchAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ContrapartidaDispatchAttempts_ResponseCatalogId",
                table: "ContrapartidaDispatchAttempts");

            migrationBuilder.DropColumn(
                name: "BusinessStatus",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "ProcessedAtUtc",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "RequiresManualReview",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "ResponseCatalogId",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "RetryAllowed",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "TransportStatus",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "BusinessStatus",
                table: "ContrapartidaDispatchAttempts");

            migrationBuilder.DropColumn(
                name: "ProcessedAtUtc",
                table: "ContrapartidaDispatchAttempts");

            migrationBuilder.DropColumn(
                name: "RequiresManualReview",
                table: "ContrapartidaDispatchAttempts");

            migrationBuilder.DropColumn(
                name: "ResponseCatalogId",
                table: "ContrapartidaDispatchAttempts");

            migrationBuilder.DropColumn(
                name: "RetryAllowed",
                table: "ContrapartidaDispatchAttempts");

            migrationBuilder.DropColumn(
                name: "TransportStatus",
                table: "ContrapartidaDispatchAttempts");
        }
    }
}
