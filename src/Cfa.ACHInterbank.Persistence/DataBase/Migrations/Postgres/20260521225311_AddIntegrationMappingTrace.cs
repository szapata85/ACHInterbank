using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddIntegrationMappingTrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationMappingTraces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OperationKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MappingPurpose = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MappingDirection = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    Reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    MappingSetId = table.Column<Guid>(type: "uuid", nullable: true),
                    MappingVersion = table.Column<int>(type: "integer", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalTransmission = table.Column<bool>(type: "boolean", nullable: false),
                    MonetaryMovementCreated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMappingTraces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationMappingTraceEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TraceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceField = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TargetField = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SourceValueSanitized = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MappedValueSanitized = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MappingRuleId = table.Column<long>(type: "bigint", nullable: true),
                    TransformationApplied = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DefaultValueApplied = table.Column<bool>(type: "boolean", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    UsedFallback = table.Column<bool>(type: "boolean", nullable: false),
                    Missing = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMappingTraceEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationMappingTraceEntries_IntegrationMappingTraces_Tra~",
                        column: x => x.TraceId,
                        principalTable: "IntegrationMappingTraces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingTraceEntries_TraceId_TargetField",
                table: "IntegrationMappingTraceEntries",
                columns: new[] { "TraceId", "TargetField" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingTraces_CorrelationId",
                table: "IntegrationMappingTraces",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingTraces_IntegrationKey_OperationKey_Create~",
                table: "IntegrationMappingTraces",
                columns: new[] { "IntegrationKey", "OperationKey", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingTraces_TransactionId",
                table: "IntegrationMappingTraces",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationMappingTraceEntries");

            migrationBuilder.DropTable(
                name: "IntegrationMappingTraces");
        }
    }
}
