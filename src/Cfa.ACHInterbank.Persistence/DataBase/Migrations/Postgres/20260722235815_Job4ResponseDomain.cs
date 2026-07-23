using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Job4ResponseDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClearingHouseId",
                table: "AchResponseStatusMappings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "AchResponseStatusMappings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "AchResponseStatusMappings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "AppliedMappingId",
                table: "AchResponses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalPayloadHash",
                table: "AchResponses",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ClearingHouseId",
                table: "AchResponses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DuplicateReceiptCount",
                table: "AchResponses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OperationalDate",
                table: "AchResponses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "AchResponses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                UPDATE "AchResponseStatusMappings"
                SET "Version" = md5('ach-response-mapping:' || "Id"::text)::uuid;

                UPDATE "AchResponseStatusMappings" AS m
                SET "ClearingHouseId" = c."Id"
                FROM "ClearingHouses" AS c
                WHERE UPPER(TRIM(m."CodigoCamaraCompensacion")) = c."Code";

                UPDATE "AchResponses"
                SET "CanonicalPayloadHash" = "HashIdempotencia",
                    "OperationalDate" = date_trunc('day', "FechaRecepcion"),
                    "Version" = "Id";

                UPDATE "AchResponses" AS r
                SET "ClearingHouseId" = c."Id"
                FROM "ClearingHouses" AS c
                WHERE UPPER(TRIM(r."CodigoCamaraCompensacion")) = c."Code";
                """);

            migrationBuilder.CreateTable(
                name: "AchResponseAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AchResponseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PreviousState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NewState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Actor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SanitizedMetadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchResponseAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchResponseAudits_AchResponses_AchResponseId",
                        column: x => x.AchResponseId,
                        principalTable: "AchResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchResponseOrphans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AchResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    ResponseType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExternalIdentifiers = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExternalCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperationalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CanonicalPayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrphanReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CandidateReferences = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolutionStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResolvedReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ResolutionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchResponseOrphans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchResponseOrphans_AchResponses_AchResponseId",
                        column: x => x.AchResponseId,
                        principalTable: "AchResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchResponseOrphans_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchResponseReconciliationCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    AchResponseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExceptionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Resolution = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResolutionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchResponseReconciliationCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchResponseReconciliationCases_AchResponses_AchResponseId",
                        column: x => x.AchResponseId,
                        principalTable: "AchResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchResponseReconciliationCases_ClearingHouses_ClearingHouse~",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchResponseReprocessAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AchResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Result = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchResponseReprocessAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchResponseReprocessAttempts_AchResponses_AchResponseId",
                        column: x => x.AchResponseId,
                        principalTable: "AchResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchRespStatusMap_Resolution",
                table: "AchResponseStatusMappings",
                columns: new[] { "ClearingHouseId", "TipoRespuesta", "CodigoEstadoExterno", "CodigoCausalExterna", "Priority", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_AchResponses_AppliedMappingId",
                table: "AchResponses",
                column: "AppliedMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_AchResponses_Operational",
                table: "AchResponses",
                columns: new[] { "ClearingHouseId", "OperationalDate", "EstadoProcesamiento" });

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseAudits_AchResponseId",
                table: "AchResponseAudits",
                column: "AchResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseAudits_CorrelationId",
                table: "AchResponseAudits",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseAudits_EntityType_EntityId_OccurredAtUtc",
                table: "AchResponseAudits",
                columns: new[] { "EntityType", "EntityId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseOrphans_AchResponseId",
                table: "AchResponseOrphans",
                column: "AchResponseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseOrphans_ClearingHouseId_ResolutionStatus_Receive~",
                table: "AchResponseOrphans",
                columns: new[] { "ClearingHouseId", "ResolutionStatus", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseReconciliationCases_AchResponseId",
                table: "AchResponseReconciliationCases",
                column: "AchResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseReconciliationCases_ClearingHouseId_Status_Excep~",
                table: "AchResponseReconciliationCases",
                columns: new[] { "ClearingHouseId", "Status", "ExceptionType" });

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseReconciliationCases_CorrelationId",
                table: "AchResponseReconciliationCases",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseReprocessAttempts_AchResponseId_AttemptNumber",
                table: "AchResponseReprocessAttempts",
                columns: new[] { "AchResponseId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseReprocessAttempts_AchResponseId_Status",
                table: "AchResponseReprocessAttempts",
                columns: new[] { "AchResponseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AchResponseReprocessAttempts_CommandId",
                table: "AchResponseReprocessAttempts",
                column: "CommandId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AchResponses_AchResponseStatusMappings_AppliedMappingId",
                table: "AchResponses",
                column: "AppliedMappingId",
                principalTable: "AchResponseStatusMappings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AchResponses_ClearingHouses_ClearingHouseId",
                table: "AchResponses",
                column: "ClearingHouseId",
                principalTable: "ClearingHouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AchResponseStatusMappings_ClearingHouses_ClearingHouseId",
                table: "AchResponseStatusMappings",
                column: "ClearingHouseId",
                principalTable: "ClearingHouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AchResponses_AchResponseStatusMappings_AppliedMappingId",
                table: "AchResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_AchResponses_ClearingHouses_ClearingHouseId",
                table: "AchResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_AchResponseStatusMappings_ClearingHouses_ClearingHouseId",
                table: "AchResponseStatusMappings");

            migrationBuilder.DropTable(
                name: "AchResponseAudits");

            migrationBuilder.DropTable(
                name: "AchResponseOrphans");

            migrationBuilder.DropTable(
                name: "AchResponseReconciliationCases");

            migrationBuilder.DropTable(
                name: "AchResponseReprocessAttempts");

            migrationBuilder.DropIndex(
                name: "IX_AchRespStatusMap_Resolution",
                table: "AchResponseStatusMappings");

            migrationBuilder.DropIndex(
                name: "IX_AchResponses_AppliedMappingId",
                table: "AchResponses");

            migrationBuilder.DropIndex(
                name: "IX_AchResponses_Operational",
                table: "AchResponses");

            migrationBuilder.DropColumn(
                name: "ClearingHouseId",
                table: "AchResponseStatusMappings");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "AchResponseStatusMappings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AchResponseStatusMappings");

            migrationBuilder.DropColumn(
                name: "AppliedMappingId",
                table: "AchResponses");

            migrationBuilder.DropColumn(
                name: "CanonicalPayloadHash",
                table: "AchResponses");

            migrationBuilder.DropColumn(
                name: "ClearingHouseId",
                table: "AchResponses");

            migrationBuilder.DropColumn(
                name: "DuplicateReceiptCount",
                table: "AchResponses");

            migrationBuilder.DropColumn(
                name: "OperationalDate",
                table: "AchResponses");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AchResponses");
        }
    }
}
