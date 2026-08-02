using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
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
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClearingHouseId",
                table: "AchTransactionStateEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "AchTransactionStateEvents",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAtUtc",
                table: "AchTransactionStateEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedReasonDescription",
                table: "AchTransactionStateEvents",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [AchTransactions] WHERE LEN([TraceNumber]) > 20)
                    THROW 51001, 'FASE1A_TRACE_NUMBER_TOO_LONG: existen trazas con más de 20 caracteres; no se realizará truncamiento.', 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "TraceNumber",
                table: "AchTransactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ClassificationStatus",
                table: "AchTransactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<int>(
                name: "ClassificationVersion",
                table: "AchTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassifiedAtUtc",
                table: "AchTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "AchTransactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "MonetaryIntegrationRoute",
                table: "AchTransactions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ManualReview");

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "AchTransactions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<bool>(
                name: "SourceInstitutionWasDefaultAtCreation",
                table: "AchTransactions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AchTransactionId",
                table: "AchResponses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationCriterion",
                table: "AchResponses",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationStatus",
                table: "AchResponses",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<int>(
                name: "ClearingHouseId",
                table: "AchFileRejectionCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "AchFileRejectionCodes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "AchFileRejectionCodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulatorySource",
                table: "AchFileRejectionCodes",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Catálogo histórico pendiente de contexto");

            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAtUtc",
                table: "AchFileExports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcknowledgementCode",
                table: "AchFileExports",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentSha256",
                table: "AchFileExports",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LifecycleStatus",
                table: "AchFileExports",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "HistoricalUnknown");

            migrationBuilder.AddColumn<string>(
                name: "TransmissionReference",
                table: "AchFileExports",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransmittedAtUtc",
                table: "AchFileExports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AchFileExports",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [AchTransactionStateEvents]
                SET [OccurredAtUtc] = CAST([CreatedAt] AS datetime2)
                WHERE [OccurredAtUtc] IS NULL;

                UPDATE codes
                SET [ClearingHouseId] = chamber.[Id],
                    [RegulatorySource] = N'CENIT DSP-152 Anexo B; MATRIZ_REGLAS_CENIT'
                FROM [AchFileRejectionCodes] codes
                CROSS APPLY (
                    SELECT TOP (1) [Id]
                    FROM [ClearingHouses]
                    WHERE UPPER([Code]) = 'CENIT'
                    ORDER BY [Id]
                ) chamber
                WHERE codes.[ClearingHouseId] IS NULL
                  AND codes.[Code] LIKE 'D%';

                UPDATE codes
                SET [Description] = N'El archivo está dirigido a una entidad receptora diferente de la esperada.'
                FROM [AchFileRejectionCodes] codes
                INNER JOIN [ClearingHouses] chamber ON chamber.[Id] = codes.[ClearingHouseId]
                WHERE codes.[Code] = 'D01' AND UPPER(chamber.[Code]) = 'CENIT';

                UPDATE codes
                SET [Description] = N'El archivo fue firmado o cifrado para un operador receptor o usuarios no válidos.', [AppliesToStage] = 'Protection'
                FROM [AchFileRejectionCodes] codes
                INNER JOIN [ClearingHouses] chamber ON chamber.[Id] = codes.[ClearingHouseId]
                WHERE codes.[Code] = 'D02' AND UPPER(chamber.[Code]) = 'CENIT';

                UPDATE codes
                SET [Description] = N'El archivo tiene formato incorrecto y no fue posible procesarlo.', [AppliesToStage] = 'Parser'
                FROM [AchFileRejectionCodes] codes
                INNER JOIN [ClearingHouses] chamber ON chamber.[Id] = codes.[ClearingHouseId]
                WHERE codes.[Code] = 'D03' AND UPPER(chamber.[Code]) = 'CENIT';

                UPDATE codes
                SET [Description] = N'El archivo ya fue recibido y corresponde a un duplicado.'
                FROM [AchFileRejectionCodes] codes
                INNER JOIN [ClearingHouses] chamber ON chamber.[Id] = codes.[ClearingHouseId]
                WHERE codes.[Code] = 'D04' AND UPPER(chamber.[Code]) = 'CENIT';

                UPDATE codes
                SET [Description] = N'El número de registros del nombre externo no coincide con el contenido del archivo.', [AppliesToStage] = 'Validation'
                FROM [AchFileRejectionCodes] codes
                INNER JOIN [ClearingHouses] chamber ON chamber.[Id] = codes.[ClearingHouseId]
                WHERE codes.[Code] = 'D05' AND UPPER(chamber.[Code]) = 'CENIT';

                UPDATE codes
                SET [Description] = N'La distribución del archivo no corresponde al operador receptor según las reglas vigentes.', [AppliesToStage] = 'Validation'
                FROM [AchFileRejectionCodes] codes
                INNER JOIN [ClearingHouses] chamber ON chamber.[Id] = codes.[ClearingHouseId]
                WHERE codes.[Code] = 'D06' AND UPPER(chamber.[Code]) = 'CENIT';

                UPDATE [AchReturnCodes]
                SET [IsActive] = 0,
                    [BusinessOutcome] = 'NotProcessed',
                    [RegulatorySource] = N'R96_INTEGRATION_ONLY'
                WHERE UPPER([Code]) = 'R96'
                  AND [FlowType] = 'Any'
                  AND [AppliesToReturn] = 0
                  AND [EffectiveFrom] <= '2000-01-01';

                IF EXISTS (SELECT 1 FROM [LiquidityOptimizationDecisions] GROUP BY [CenitCycleExecutionId], [AchTransactionId] HAVING COUNT(*) > 1)
                    THROW 51002, 'FASE1A_DUPLICATE_LIQUIDITY_DECISION: se requiere depuración explícita antes de crear la unicidad.', 1;
                IF EXISTS (SELECT 1 FROM [CenitCycleQueues] WHERE [Status] = 'Queued' GROUP BY [AchTransactionId], [TargetAchCycleId] HAVING COUNT(*) > 1)
                    THROW 51003, 'FASE1A_DUPLICATE_ACTIVE_CYCLE_QUEUE: se requiere revisión antes de crear la unicidad.', 1;
                IF EXISTS (SELECT 1 FROM [AchFileExports] GROUP BY [AchCycleId], [ExportKind], [IsEncrypted], [FileName] HAVING COUNT(*) > 1)
                    THROW 51004, 'FASE1A_DUPLICATE_FILE_EXPORT: se requiere revisión antes de crear la unicidad.', 1;
                IF EXISTS (SELECT 1 FROM [AchFileRejectionCodes] WHERE [ClearingHouseId] IS NOT NULL GROUP BY [ClearingHouseId], [Code], [AppliesToStage], [EffectiveFrom] HAVING COUNT(*) > 1)
                    THROW 51005, 'FASE1A_DUPLICATE_FILE_REJECTION_CODE: se requiere revisión antes de crear la unicidad.', 1;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OccurredAtUtc",
                table: "AchTransactionStateEvents",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AchFileExportTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchFileExportId = table.Column<int>(type: "int", nullable: false),
                    AchTransactionId = table.Column<int>(type: "int", nullable: false),
                    AchCycleId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AchBatchId = table.Column<int>(type: "int", nullable: false),
                    FileSequence = table.Column<int>(type: "int", nullable: false),
                    TraceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IncludedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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
                name: "IX_LiquidityOptimizationDecisions_CenitCycleExecutionId_AchTransactionId",
                table: "LiquidityOptimizationDecisions",
                columns: new[] { "CenitCycleExecutionId", "AchTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CenitCycleQueues_ActiveTarget",
                table: "CenitCycleQueues",
                columns: new[] { "AchTransactionId", "TargetAchCycleId" },
                unique: true,
                filter: "[Status] = 'Queued'");

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
                filter: "[IdempotencyKey] IS NOT NULL");

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
                name: "IX_AchFileRejectionCodes_ClearingHouseId_Code_AppliesToStage_EffectiveFrom",
                table: "AchFileRejectionCodes",
                columns: new[] { "ClearingHouseId", "Code", "AppliesToStage", "EffectiveFrom" },
                unique: true,
                filter: "[ClearingHouseId] IS NOT NULL");

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
                filter: "[Version] IS NOT NULL");

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
                UPDATE [AchFileRejectionCodes]
                SET [Description] = N'Archivo duplicado detectado por hash/tamaño.'
                WHERE [Code] = 'D01';
                UPDATE [AchFileRejectionCodes]
                SET [Description] = N'Formato o estructura de archivo inválida.', [AppliesToStage] = 'Parser'
                WHERE [Code] = 'D02';
                UPDATE [AchFileRejectionCodes]
                SET [Description] = N'Operador o canal de transmisión incorrecto.', [AppliesToStage] = 'Transmission'
                WHERE [Code] = 'D03';
                UPDATE [AchFileRejectionCodes]
                SET [Description] = N'Inconsistencia de secuencia, batch count o conteos físicos.'
                WHERE [Code] = 'D04';
                UPDATE [AchFileRejectionCodes]
                SET [Description] = N'Los controles o totales del archivo no son válidos.', [AppliesToStage] = 'Validation'
                WHERE [Code] = 'D05';
                UPDATE [AchFileRejectionCodes]
                SET [Description] = N'Existe un campo obligatorio ausente o un registro fuera del orden esperado.', [AppliesToStage] = 'Parser'
                WHERE [Code] = 'D06';
                UPDATE r
                SET [IsActive] = 1,
                    [BusinessOutcome] = 'Successful',
                    [RegulatorySource] = ch.[Code]
                FROM [AchReturnCodes] r
                INNER JOIN [ClearingHouses] ch ON ch.[Id] = r.[ClearingHouseId]
                WHERE UPPER(r.[Code]) = 'R96'
                  AND r.[RegulatorySource] = N'R96_INTEGRATION_ONLY';
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
                name: "IX_LiquidityOptimizationDecisions_CenitCycleExecutionId_AchTransactionId",
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
                name: "IX_AchFileRejectionCodes_ClearingHouseId_Code_AppliesToStage_EffectiveFrom",
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
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
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
