using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class OutgoingTransactionTraceabilityPhase1A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CenitCycleQueues_AchTransactionId",
                table: "CenitCycleQueues");

            migrationBuilder.DropIndex(
                name: "IX_AchFileRejectionCodes_Code",
                table: "AchFileRejectionCodes");

            migrationBuilder.AddColumn<int>(
                name: "AchReturnCodeId",
                table: "AchTransactionStateEvents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClearingHouseId",
                table: "AchTransactionStateEvents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "AchTransactionStateEvents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAtUtc",
                table: "AchTransactionStateEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedReasonDescription",
                table: "AchTransactionStateEvents",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "AchTransactions" WHERE length("TraceNumber") > 20) THEN
                        RAISE EXCEPTION 'FASE1A_TRACE_NUMBER_TOO_LONG: existen trazas con más de 20 caracteres; no se realizará truncamiento.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "TraceNumber",
                table: "AchTransactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ClassificationStatus",
                table: "AchTransactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<int>(
                name: "ClassificationVersion",
                table: "AchTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassifiedAtUtc",
                table: "AchTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "AchTransactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "MonetaryIntegrationRoute",
                table: "AchTransactions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ManualReview");

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "AchTransactions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<bool>(
                name: "SourceInstitutionWasDefaultAtCreation",
                table: "AchTransactions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AchTransactionId",
                table: "AchResponses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationCriterion",
                table: "AchResponses",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationStatus",
                table: "AchResponses",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<int>(
                name: "ClearingHouseId",
                table: "AchFileRejectionCodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "AchFileRejectionCodes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "AchFileRejectionCodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulatorySource",
                table: "AchFileRejectionCodes",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Catálogo histórico pendiente de contexto");

            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAtUtc",
                table: "AchFileExports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcknowledgementCode",
                table: "AchFileExports",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentSha256",
                table: "AchFileExports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LifecycleStatus",
                table: "AchFileExports",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "HistoricalUnknown");

            migrationBuilder.AddColumn<string>(
                name: "TransmissionReference",
                table: "AchFileExports",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransmittedAtUtc",
                table: "AchFileExports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AchFileExports",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "AchTransactionStateEvents"
                SET "OccurredAtUtc" = "CreatedAt"
                WHERE "OccurredAtUtc" IS NULL;

                UPDATE "AchFileRejectionCodes"
                SET "ClearingHouseId" = (
                        SELECT "Id"
                        FROM "ClearingHouses"
                        WHERE upper("Code") = 'CENIT'
                        ORDER BY "Id"
                        LIMIT 1),
                    "RegulatorySource" = 'CENIT DSP-152 Anexo B; MATRIZ_REGLAS_CENIT'
                WHERE "ClearingHouseId" IS NULL
                  AND "Code" LIKE 'D%';

                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'El archivo está dirigido a una entidad receptora diferente de la esperada.'
                WHERE "Code" = 'D01'
                  AND "ClearingHouseId" = (SELECT "Id" FROM "ClearingHouses" WHERE upper("Code") = 'CENIT' ORDER BY "Id" LIMIT 1);

                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'El archivo fue firmado o cifrado para un operador receptor o usuarios no válidos.', "AppliesToStage" = 'Protection'
                WHERE "Code" = 'D02'
                  AND "ClearingHouseId" = (SELECT "Id" FROM "ClearingHouses" WHERE upper("Code") = 'CENIT' ORDER BY "Id" LIMIT 1);

                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'El archivo tiene formato incorrecto y no fue posible procesarlo.', "AppliesToStage" = 'Parser'
                WHERE "Code" = 'D03'
                  AND "ClearingHouseId" = (SELECT "Id" FROM "ClearingHouses" WHERE upper("Code") = 'CENIT' ORDER BY "Id" LIMIT 1);

                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'El archivo ya fue recibido y corresponde a un duplicado.'
                WHERE "Code" = 'D04'
                  AND "ClearingHouseId" = (SELECT "Id" FROM "ClearingHouses" WHERE upper("Code") = 'CENIT' ORDER BY "Id" LIMIT 1);

                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'El número de registros del nombre externo no coincide con el contenido del archivo.', "AppliesToStage" = 'Validation'
                WHERE "Code" = 'D05'
                  AND "ClearingHouseId" = (SELECT "Id" FROM "ClearingHouses" WHERE upper("Code") = 'CENIT' ORDER BY "Id" LIMIT 1);

                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'La distribución del archivo no corresponde al operador receptor según las reglas vigentes.', "AppliesToStage" = 'Validation'
                WHERE "Code" = 'D06'
                  AND "ClearingHouseId" = (SELECT "Id" FROM "ClearingHouses" WHERE upper("Code") = 'CENIT' ORDER BY "Id" LIMIT 1);

                UPDATE "AchReturnCodes"
                SET "IsActive" = FALSE,
                    "BusinessOutcome" = 'NotProcessed',
                    "RegulatorySource" = 'R96_INTEGRATION_ONLY'
                WHERE upper("Code") = 'R96'
                  AND "FlowType" = 'Any'
                  AND "AppliesToReturn" = FALSE
                  AND "EffectiveFrom" <= TIMESTAMPTZ '2000-01-01';

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "LiquidityOptimizationDecisions" GROUP BY "CenitCycleExecutionId", "AchTransactionId" HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'FASE1A_DUPLICATE_LIQUIDITY_DECISION: se requiere depuración explícita antes de crear la unicidad.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "CenitCycleQueues" WHERE "Status" = 'Queued' GROUP BY "AchTransactionId", "TargetAchCycleId" HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'FASE1A_DUPLICATE_ACTIVE_CYCLE_QUEUE: se requiere revisión antes de crear la unicidad.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "AchFileExports" GROUP BY "AchCycleId", "ExportKind", "IsEncrypted", "FileName" HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'FASE1A_DUPLICATE_FILE_EXPORT: se requiere revisión antes de crear la unicidad.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "AchFileRejectionCodes" WHERE "ClearingHouseId" IS NOT NULL GROUP BY "ClearingHouseId", "Code", "AppliesToStage", "EffectiveFrom" HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'FASE1A_DUPLICATE_FILE_REJECTION_CODE: se requiere revisión antes de crear la unicidad.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OccurredAtUtc",
                table: "AchTransactionStateEvents",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AchFileExportTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AchFileExportId = table.Column<int>(type: "integer", nullable: false),
                    AchTransactionId = table.Column<int>(type: "integer", nullable: false),
                    AchCycleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AchBatchId = table.Column<int>(type: "integer", nullable: false),
                    FileSequence = table.Column<int>(type: "integer", nullable: false),
                    TraceNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IncludedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchFileExportTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchFileExportTransactions_AchFileExports_AchFileExportId",
                        column: x => x.AchFileExportId,
                        principalTable: "AchFileExports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AchFileExportTransactions_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiquidityOptimizationDecisions_CenitCycleExecutionId_AchTra~",
                table: "LiquidityOptimizationDecisions",
                columns: new[] { "CenitCycleExecutionId", "AchTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CenitCycleQueues_ActiveTarget",
                table: "CenitCycleQueues",
                columns: new[] { "AchTransactionId", "TargetAchCycleId" },
                unique: true,
                filter: "\"Status\" = 'Queued'");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactionStateEvents_AchReturnCodeId",
                table: "AchTransactionStateEvents",
                column: "AchReturnCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactionStateEvents_AchTransactionId_OccurredAtUtc",
                table: "AchTransactionStateEvents",
                columns: new[] { "AchTransactionId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactionStateEvents_ClearingHouseId",
                table: "AchTransactionStateEvents",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "UX_AchTransactionStateEvents_IdempotencyKey",
                table: "AchTransactionStateEvents",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_Direction_Classification_CreatedAt",
                table: "AchTransactions",
                columns: new[] { "Direction", "ClassificationStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_MonetaryRoute_State",
                table: "AchTransactions",
                columns: new[] { "MonetaryIntegrationRoute", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_TraceNumber",
                table: "AchTransactions",
                column: "TraceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_TransactionExternalId",
                table: "AchTransactions",
                column: "TransactionExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_AchResponses_Transaction_ReceivedAt",
                table: "AchResponses",
                columns: new[] { "AchTransactionId", "FechaRecepcion" });

            migrationBuilder.CreateIndex(
                name: "IX_AchFileRejectionCodes_ClearingHouseId_Code_AppliesToStage_E~",
                table: "AchFileRejectionCodes",
                columns: new[] { "ClearingHouseId", "Code", "AppliesToStage", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchFileExports_ContentIdentity",
                table: "AchFileExports",
                columns: new[] { "AchCycleId", "ExportKind", "IsEncrypted", "ContentSha256" });

            migrationBuilder.CreateIndex(
                name: "UX_AchFileExports_Cycle_Kind_Encrypted_FileName",
                table: "AchFileExports",
                columns: new[] { "AchCycleId", "ExportKind", "IsEncrypted", "FileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AchFileExports_Cycle_Kind_Encrypted_Version",
                table: "AchFileExports",
                columns: new[] { "AchCycleId", "ExportKind", "IsEncrypted", "Version" },
                unique: true,
                filter: "\"Version\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AchFileExportTransactions_AchFileExportId_AchTransactionId",
                table: "AchFileExportTransactions",
                columns: new[] { "AchFileExportId", "AchTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchFileExportTransactions_AchFileExportId_FileSequence",
                table: "AchFileExportTransactions",
                columns: new[] { "AchFileExportId", "FileSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchFileExportTransactions_AchTransactionId_AchFileExportId",
                table: "AchFileExportTransactions",
                columns: new[] { "AchTransactionId", "AchFileExportId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AchFileRejectionCodes_ClearingHouses_ClearingHouseId",
                table: "AchFileRejectionCodes",
                column: "ClearingHouseId",
                principalTable: "ClearingHouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AchResponses_AchTransactions_AchTransactionId",
                table: "AchResponses",
                column: "AchTransactionId",
                principalTable: "AchTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AchTransactionStateEvents_AchReturnCodes_AchReturnCodeId",
                table: "AchTransactionStateEvents",
                column: "AchReturnCodeId",
                principalTable: "AchReturnCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AchTransactionStateEvents_ClearingHouses_ClearingHouseId",
                table: "AchTransactionStateEvents",
                column: "ClearingHouseId",
                principalTable: "ClearingHouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'Archivo duplicado detectado por hash/tamaño.'
                WHERE "Code" = 'D01';
                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'Formato o estructura de archivo inválida.', "AppliesToStage" = 'Parser'
                WHERE "Code" = 'D02';
                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'Operador o canal de transmisión incorrecto.', "AppliesToStage" = 'Transmission'
                WHERE "Code" = 'D03';
                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'Inconsistencia de secuencia, batch count o conteos físicos.'
                WHERE "Code" = 'D04';
                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'Los controles o totales del archivo no son válidos.', "AppliesToStage" = 'Validation'
                WHERE "Code" = 'D05';
                UPDATE "AchFileRejectionCodes"
                SET "Description" = 'Existe un campo obligatorio ausente o un registro fuera del orden esperado.', "AppliesToStage" = 'Parser'
                WHERE "Code" = 'D06';
                UPDATE "AchReturnCodes" r
                SET "IsActive" = TRUE,
                    "BusinessOutcome" = 'Successful',
                    "RegulatorySource" = ch."Code"
                FROM "ClearingHouses" ch
                WHERE r."ClearingHouseId" = ch."Id"
                  AND upper(r."Code") = 'R96'
                  AND r."RegulatorySource" = 'R96_INTEGRATION_ONLY';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_AchFileRejectionCodes_ClearingHouses_ClearingHouseId",
                table: "AchFileRejectionCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_AchResponses_AchTransactions_AchTransactionId",
                table: "AchResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_AchTransactionStateEvents_AchReturnCodes_AchReturnCodeId",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_AchTransactionStateEvents_ClearingHouses_ClearingHouseId",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropTable(
                name: "AchFileExportTransactions");

            migrationBuilder.DropIndex(
                name: "IX_LiquidityOptimizationDecisions_CenitCycleExecutionId_AchTra~",
                table: "LiquidityOptimizationDecisions");

            migrationBuilder.DropIndex(
                name: "UX_CenitCycleQueues_ActiveTarget",
                table: "CenitCycleQueues");

            migrationBuilder.DropIndex(
                name: "IX_AchTransactionStateEvents_AchReturnCodeId",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropIndex(
                name: "IX_AchTransactionStateEvents_AchTransactionId_OccurredAtUtc",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropIndex(
                name: "IX_AchTransactionStateEvents_ClearingHouseId",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropIndex(
                name: "UX_AchTransactionStateEvents_IdempotencyKey",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropIndex(
                name: "IX_AchTransactions_Direction_Classification_CreatedAt",
                table: "AchTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AchTransactions_MonetaryRoute_State",
                table: "AchTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AchTransactions_TraceNumber",
                table: "AchTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AchTransactions_TransactionExternalId",
                table: "AchTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AchResponses_Transaction_ReceivedAt",
                table: "AchResponses");

            migrationBuilder.DropIndex(
                name: "IX_AchFileRejectionCodes_ClearingHouseId_Code_AppliesToStage_E~",
                table: "AchFileRejectionCodes");

            migrationBuilder.DropIndex(
                name: "IX_AchFileExports_ContentIdentity",
                table: "AchFileExports");

            migrationBuilder.DropIndex(
                name: "UX_AchFileExports_Cycle_Kind_Encrypted_FileName",
                table: "AchFileExports");

            migrationBuilder.DropIndex(
                name: "UX_AchFileExports_Cycle_Kind_Encrypted_Version",
                table: "AchFileExports");

            migrationBuilder.DropColumn(
                name: "AchReturnCodeId",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropColumn(
                name: "ClearingHouseId",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropColumn(
                name: "OccurredAtUtc",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropColumn(
                name: "ResolvedReasonDescription",
                table: "AchTransactionStateEvents");

            migrationBuilder.DropColumn(
                name: "ClassificationStatus",
                table: "AchTransactions");

            migrationBuilder.DropColumn(
                name: "ClassificationVersion",
                table: "AchTransactions");

            migrationBuilder.DropColumn(
                name: "ClassifiedAtUtc",
                table: "AchTransactions");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "AchTransactions");

            migrationBuilder.DropColumn(
                name: "MonetaryIntegrationRoute",
                table: "AchTransactions");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "AchTransactions");

            migrationBuilder.DropColumn(
                name: "SourceInstitutionWasDefaultAtCreation",
                table: "AchTransactions");

            migrationBuilder.DropColumn(
                name: "AchTransactionId",
                table: "AchResponses");

            migrationBuilder.DropColumn(
                name: "CorrelationCriterion",
                table: "AchResponses");

            migrationBuilder.DropColumn(
                name: "CorrelationStatus",
                table: "AchResponses");

            migrationBuilder.DropColumn(
                name: "ClearingHouseId",
                table: "AchFileRejectionCodes");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "AchFileRejectionCodes");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "AchFileRejectionCodes");

            migrationBuilder.DropColumn(
                name: "RegulatorySource",
                table: "AchFileRejectionCodes");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAtUtc",
                table: "AchFileExports");

            migrationBuilder.DropColumn(
                name: "AcknowledgementCode",
                table: "AchFileExports");

            migrationBuilder.DropColumn(
                name: "ContentSha256",
                table: "AchFileExports");

            migrationBuilder.DropColumn(
                name: "LifecycleStatus",
                table: "AchFileExports");

            migrationBuilder.DropColumn(
                name: "TransmissionReference",
                table: "AchFileExports");

            migrationBuilder.DropColumn(
                name: "TransmittedAtUtc",
                table: "AchFileExports");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AchFileExports");

            migrationBuilder.AlterColumn<string>(
                name: "TraceNumber",
                table: "AchTransactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_CenitCycleQueues_AchTransactionId",
                table: "CenitCycleQueues",
                column: "AchTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AchFileRejectionCodes_Code",
                table: "AchFileRejectionCodes",
                column: "Code",
                unique: true);
        }
    }
}
