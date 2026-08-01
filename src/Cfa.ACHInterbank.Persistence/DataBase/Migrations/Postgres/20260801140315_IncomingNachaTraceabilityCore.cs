using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class IncomingNachaTraceabilityCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AddendaRecords_NachaHeaders_NachaID",
                table: "AddendaRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchControls_NachaHeaders_NachaID",
                table: "BatchControls");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchHeaders_NachaHeaders_NachaID",
                table: "BatchHeaders");

            migrationBuilder.DropForeignKey(
                name: "FK_EntryDetails_NachaHeaders_NachaID",
                table: "EntryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_FileControls_NachaHeaders_NachaID",
                table: "FileControls");

            migrationBuilder.DropIndex(
                name: "IX_NachaHeaders_ClearingHouseId",
                table: "NachaHeaders");

            migrationBuilder.DropIndex(
                name: "IX_EntryDetails_NachaID",
                table: "EntryDetails");

            migrationBuilder.DropIndex(
                name: "IX_BatchHeaders_NachaID",
                table: "BatchHeaders");

            migrationBuilder.DropIndex(
                name: "IX_AddendaRecords_NachaID",
                table: "AddendaRecords");

            migrationBuilder.DropIndex(
                name: "IX_AchReturnCodes_ClearingHouseId_Code_FlowType_EffectiveFrom",
                table: "AchReturnCodes");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "NachaHeaders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "NachaHeaders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "AchReturnCodeId",
                table: "IncomingNachaIntegrationExecution",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "IncomingNachaIntegrationExecution",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BusinessOutcome",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ClearingHouseId",
                table: "IncomingNachaIntegrationExecution",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntryDetailId",
                table: "IncomingNachaIntegrationExecution",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalTransactionId",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultCode",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultDescription",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultSource",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TechnicalErrorCode",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TechnicalErrorMessage",
                table: "IncomingNachaIntegrationExecution",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DetectedCycleNumber",
                table: "IncomingNachaFileIngestions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveDate",
                table: "IncomingNachaFileIngestions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileExtension",
                table: "IncomingNachaFileIngestions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FileNameDate",
                table: "IncomingNachaFileIngestions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HeaderDate",
                table: "IncomingNachaFileIngestions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileCode",
                table: "IncomingNachaFileIngestions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileVersion",
                table: "IncomingNachaFileIngestions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionCode",
                table: "IncomingNachaFileIngestions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionDescription",
                table: "IncomingNachaFileIngestions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionTitle",
                table: "IncomingNachaFileIngestions",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "IncomingNachaFileIngestions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SuggestedAction",
                table: "IncomingNachaFileIngestions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalErrorCode",
                table: "IncomingNachaFileIngestions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalErrorMessage",
                table: "IncomingNachaFileIngestions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "FileControls",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "FileControls",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "BatchHeaderId",
                table: "EntryDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "EntryDetails",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "EntryDetails",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "BatchHeaders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "BatchHeaders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "BatchHeaderId",
                table: "BatchControls",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "BatchControls",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "BatchControls",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AddendaRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "EntryDetailId",
                table: "AddendaRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AddendaRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "BusinessOutcome",
                table: "AchReturnCodes",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AllowsExplicitReprocessing",
                table: "AchCycles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OperationalStatus",
                table: "AchCycles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReceptionToleranceMinutes",
                table: "AchCycles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill conservador: cuando existe una fecha de recepción se usa como
            // origen de auditoría; para legado sin correlación, CURRENT_TIMESTAMP
            // representa el momento desde el cual la auditoría quedó disponible.
            migrationBuilder.Sql("""
                UPDATE "NachaHeaders" h
                SET "CreatedAt" = COALESCE(i."ReceivedAtUtc", i."UploadedAtUtc", CURRENT_TIMESTAMP),
                    "UpdatedAt" = COALESCE(i."ReceivedAtUtc", i."UploadedAtUtc", CURRENT_TIMESTAMP)
                FROM "IncomingNachaFileIngestions" i
                WHERE i."Id" = h."IncomingNachaFileIngestionId";
                UPDATE "NachaHeaders" SET "CreatedAt" = CURRENT_TIMESTAMP, "UpdatedAt" = CURRENT_TIMESTAMP
                WHERE "CreatedAt" < TIMESTAMPTZ '1900-01-01';

                UPDATE "BatchHeaders" b SET "CreatedAt" = h."CreatedAt", "UpdatedAt" = h."UpdatedAt"
                FROM "NachaHeaders" h WHERE h."NachaID" = b."NachaID";
                UPDATE "EntryDetails" e SET "CreatedAt" = h."CreatedAt", "UpdatedAt" = h."UpdatedAt"
                FROM "NachaHeaders" h WHERE h."NachaID" = e."NachaID";
                UPDATE "AddendaRecords" a SET "CreatedAt" = h."CreatedAt", "UpdatedAt" = h."UpdatedAt"
                FROM "NachaHeaders" h WHERE h."NachaID" = a."NachaID";
                UPDATE "BatchControls" b SET "CreatedAt" = h."CreatedAt", "UpdatedAt" = h."UpdatedAt"
                FROM "NachaHeaders" h WHERE h."NachaID" = b."NachaID";
                UPDATE "FileControls" f SET "CreatedAt" = h."CreatedAt", "UpdatedAt" = h."UpdatedAt"
                FROM "NachaHeaders" h WHERE h."NachaID" = f."NachaID";

                UPDATE "EntryDetails" e SET "BatchHeaderId" = b."BatchID"
                FROM "BatchHeaders" b
                WHERE b."NachaID" = e."NachaID" AND b."BatchNumber" = e."BatchNumber";
                UPDATE "BatchControls" c SET "BatchHeaderId" = b."BatchID"
                FROM "BatchHeaders" b
                WHERE b."NachaID" = c."NachaID"
                  AND c."BatchNumber" ~ '^\d+$'
                  AND b."BatchNumber" = CAST(c."BatchNumber" AS integer);
                UPDATE "AddendaRecords" a SET "EntryDetailId" = e."EntryDetailID"
                FROM "EntryDetails" e
                WHERE e."NachaID" = a."NachaID"
                  AND RIGHT(COALESCE(e."SequenceNumber", ''), 7) = COALESCE(a."EntryDetailSequenceNumber", '');

                UPDATE "IncomingNachaFileIngestions"
                SET "FileExtension" = CASE WHEN UPPER("FileName") LIKE '%.OUT' THEN '.OUT' ELSE '' END,
                    "Stage" = CASE
                        WHEN "IngestionStatus" = 'Completado' THEN 'Persisted'
                        WHEN "IngestionStatus" = 'Bloqueado' THEN 'Rejected'
                        WHEN "IngestionStatus" = 'Fallido' THEN 'Failed'
                        ELSE 'Received' END;
                UPDATE "AchCycles" SET "OperationalStatus" = 2;
                UPDATE "AchReturnCodes" SET "BusinessOutcome" = CASE WHEN "Code" = 'R96' THEN 'Successful' ELSE 'Returned' END;

                WITH ranked AS (
                    SELECT x."Id", c."EntryDetailId", q."ClearingHouseId",
                           ROW_NUMBER() OVER (PARTITION BY c."EntryDetailId" ORDER BY x."StartedAtUtc", x."Id") AS attempt
                    FROM "IncomingNachaIntegrationExecution" x
                    JOIN "IncomingNachaDispatchQueue" q ON q."Id" = x."DispatchQueueId"
                    JOIN "IncomingNachaEntryClassifications" c ON c."Id" = q."IncomingNachaEntryClassificationId"
                )
                UPDATE "IncomingNachaIntegrationExecution" x
                SET "EntryDetailId" = r."EntryDetailId",
                    "ClearingHouseId" = r."ClearingHouseId",
                    "AttemptNumber" = r.attempt,
                    "ProcessingStatus" = CASE
                        WHEN x."IsTechnicalFailure" THEN 'TechnicalFailed'
                        WHEN x."FinishedAtUtc" IS NULL THEN 'Processing'
                        ELSE 'Completed' END,
                    "BusinessOutcome" = CASE
                        WHEN x."IsTechnicalFailure" THEN 'NotProcessed'
                        WHEN x."BusinessStatus" = 'Success' THEN 'Successful'
                        WHEN x."BusinessStatus" = 'Rejected' THEN 'Rejected'
                        ELSE 'PendingResponse' END,
                    "ResultCode" = CASE WHEN x."TransportStatus" = 'Succeeded' THEN COALESCE(x."SoapResponseCode", '') ELSE '' END,
                    "ResultDescription" = CASE WHEN x."TransportStatus" = 'Succeeded' THEN COALESCE(x."SoapResponseDescription", '') ELSE '' END,
                    "ResultSource" = 'SOAP',
                    "TechnicalErrorCode" = CASE WHEN x."IsTechnicalFailure" THEN COALESCE(x."SoapResponseCode", '') ELSE '' END,
                    "TechnicalErrorMessage" = CASE WHEN x."IsTechnicalFailure" THEN COALESCE(x."SoapResponseDescription", '') ELSE '' END
                FROM ranked r WHERE r."Id" = x."Id";

                INSERT INTO "AchReturnCodes"
                    ("ClearingHouseId", "Code", "FlowType", "Description", "BusinessOutcome",
                     "AppliesToDebit", "AppliesToCredit", "AppliesToPrenotification", "AppliesToReturn",
                     "RequiresAddenda", "EffectiveFrom", "IsActive", "RegulatorySource", "CreatedAt", "UpdatedAt")
                SELECT ch."Id", 'R96', 'Any', 'Transacción procesada exitosamente', 'Successful',
                       TRUE, TRUE, FALSE, FALSE, FALSE, TIMESTAMPTZ '2000-01-01', TRUE, ch."Code", CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM "ClearingHouses" ch
                WHERE UPPER(ch."Code") IN ('ACHCOL', 'CENIT')
                  AND NOT EXISTS (SELECT 1 FROM "AchReturnCodes" r WHERE r."ClearingHouseId" = ch."Id" AND r."Code" = 'R96' AND r."FlowType" = 'Any');

                UPDATE "IncomingNachaIntegrationExecution" x SET "AchReturnCodeId" = r."Id"
                FROM "AchReturnCodes" r
                WHERE r."ClearingHouseId" = x."ClearingHouseId"
                  AND r."Code" = x."ResultCode"
                  AND r."FlowType" = 'Any'
                  AND r."IsActive" = TRUE;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_NachaHeaders_ClearingHouseId_FileCreationDate_CycleNumber",
                table: "NachaHeaders",
                columns: new[] { "ClearingHouseId", "FileCreationDate", "CycleNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_AchReturnCodeId",
                table: "IncomingNachaIntegrationExecution",
                column: "AchReturnCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_ClearingHouseId_ResultCode",
                table: "IncomingNachaIntegrationExecution",
                columns: new[] { "ClearingHouseId", "ResultCode" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_EntryDetailId_AttemptNumb~",
                table: "IncomingNachaIntegrationExecution",
                columns: new[] { "EntryDetailId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaFileIngestions_Stage_OperationalDate",
                table: "IncomingNachaFileIngestions",
                columns: new[] { "Stage", "OperationalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EntryDetails_BatchHeaderId",
                table: "EntryDetails",
                column: "BatchHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_EntryDetails_NachaID_SequenceNumber",
                table: "EntryDetails",
                columns: new[] { "NachaID", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntryDetails_SequenceNumber",
                table: "EntryDetails",
                column: "SequenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_BatchHeaders_NachaID_BatchNumber",
                table: "BatchHeaders",
                columns: new[] { "NachaID", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatchControls_BatchHeaderId",
                table: "BatchControls",
                column: "BatchHeaderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddendaRecords_EntryDetailId",
                table: "AddendaRecords",
                column: "EntryDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_AddendaRecords_NachaID_EntryDetailSequenceNumber_AddendumSe~",
                table: "AddendaRecords",
                columns: new[] { "NachaID", "EntryDetailSequenceNumber", "AddendumSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnCodes_ClearingHouseId_Code_FlowType_EffectiveFrom",
                table: "AchReturnCodes",
                columns: new[] { "ClearingHouseId", "Code", "FlowType", "EffectiveFrom" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AddendaRecords_EntryDetails_EntryDetailId",
                table: "AddendaRecords",
                column: "EntryDetailId",
                principalTable: "EntryDetails",
                principalColumn: "EntryDetailID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AddendaRecords_NachaHeaders_NachaID",
                table: "AddendaRecords",
                column: "NachaID",
                principalTable: "NachaHeaders",
                principalColumn: "NachaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchControls_BatchHeaders_BatchHeaderId",
                table: "BatchControls",
                column: "BatchHeaderId",
                principalTable: "BatchHeaders",
                principalColumn: "BatchID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchControls_NachaHeaders_NachaID",
                table: "BatchControls",
                column: "NachaID",
                principalTable: "NachaHeaders",
                principalColumn: "NachaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchHeaders_NachaHeaders_NachaID",
                table: "BatchHeaders",
                column: "NachaID",
                principalTable: "NachaHeaders",
                principalColumn: "NachaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EntryDetails_BatchHeaders_BatchHeaderId",
                table: "EntryDetails",
                column: "BatchHeaderId",
                principalTable: "BatchHeaders",
                principalColumn: "BatchID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EntryDetails_NachaHeaders_NachaID",
                table: "EntryDetails",
                column: "NachaID",
                principalTable: "NachaHeaders",
                principalColumn: "NachaID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FileControls_NachaHeaders_NachaID",
                table: "FileControls",
                column: "NachaID",
                principalTable: "NachaHeaders",
                principalColumn: "NachaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingNachaIntegrationExecution_AchReturnCodes_AchReturnC~",
                table: "IncomingNachaIntegrationExecution",
                column: "AchReturnCodeId",
                principalTable: "AchReturnCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingNachaIntegrationExecution_EntryDetails_EntryDetailId",
                table: "IncomingNachaIntegrationExecution",
                column: "EntryDetailId",
                principalTable: "EntryDetails",
                principalColumn: "EntryDetailID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AddendaRecords_EntryDetails_EntryDetailId",
                table: "AddendaRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AddendaRecords_NachaHeaders_NachaID",
                table: "AddendaRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchControls_BatchHeaders_BatchHeaderId",
                table: "BatchControls");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchControls_NachaHeaders_NachaID",
                table: "BatchControls");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchHeaders_NachaHeaders_NachaID",
                table: "BatchHeaders");

            migrationBuilder.DropForeignKey(
                name: "FK_EntryDetails_BatchHeaders_BatchHeaderId",
                table: "EntryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_EntryDetails_NachaHeaders_NachaID",
                table: "EntryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_FileControls_NachaHeaders_NachaID",
                table: "FileControls");

            migrationBuilder.DropForeignKey(
                name: "FK_IncomingNachaIntegrationExecution_AchReturnCodes_AchReturnC~",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropForeignKey(
                name: "FK_IncomingNachaIntegrationExecution_EntryDetails_EntryDetailId",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropIndex(
                name: "IX_NachaHeaders_ClearingHouseId_FileCreationDate_CycleNumber",
                table: "NachaHeaders");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaIntegrationExecution_AchReturnCodeId",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaIntegrationExecution_ClearingHouseId_ResultCode",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaIntegrationExecution_EntryDetailId_AttemptNumb~",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaFileIngestions_Stage_OperationalDate",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropIndex(
                name: "IX_EntryDetails_BatchHeaderId",
                table: "EntryDetails");

            migrationBuilder.DropIndex(
                name: "IX_EntryDetails_NachaID_SequenceNumber",
                table: "EntryDetails");

            migrationBuilder.DropIndex(
                name: "IX_EntryDetails_SequenceNumber",
                table: "EntryDetails");

            migrationBuilder.DropIndex(
                name: "IX_BatchHeaders_NachaID_BatchNumber",
                table: "BatchHeaders");

            migrationBuilder.DropIndex(
                name: "IX_BatchControls_BatchHeaderId",
                table: "BatchControls");

            migrationBuilder.DropIndex(
                name: "IX_AddendaRecords_EntryDetailId",
                table: "AddendaRecords");

            migrationBuilder.DropIndex(
                name: "IX_AddendaRecords_NachaID_EntryDetailSequenceNumber_AddendumSe~",
                table: "AddendaRecords");

            migrationBuilder.DropIndex(
                name: "IX_AchReturnCodes_ClearingHouseId_Code_FlowType_EffectiveFrom",
                table: "AchReturnCodes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "NachaHeaders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "NachaHeaders");

            migrationBuilder.DropColumn(
                name: "AchReturnCodeId",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "BusinessOutcome",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "ClearingHouseId",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "EntryDetailId",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "ExternalTransactionId",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "ResultCode",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "ResultDescription",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "ResultSource",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "TechnicalErrorCode",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "TechnicalErrorMessage",
                table: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropColumn(
                name: "DetectedCycleNumber",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "FileExtension",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "FileNameDate",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "HeaderDate",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "ProfileCode",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "ProfileVersion",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "RejectionCode",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "RejectionDescription",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "RejectionTitle",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "SuggestedAction",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "TechnicalErrorCode",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "TechnicalErrorMessage",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FileControls");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FileControls");

            migrationBuilder.DropColumn(
                name: "BatchHeaderId",
                table: "EntryDetails");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "EntryDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "EntryDetails");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BatchHeaders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BatchHeaders");

            migrationBuilder.DropColumn(
                name: "BatchHeaderId",
                table: "BatchControls");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BatchControls");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BatchControls");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AddendaRecords");

            migrationBuilder.DropColumn(
                name: "EntryDetailId",
                table: "AddendaRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AddendaRecords");

            migrationBuilder.DropColumn(
                name: "BusinessOutcome",
                table: "AchReturnCodes");

            migrationBuilder.DropColumn(
                name: "AllowsExplicitReprocessing",
                table: "AchCycles");

            migrationBuilder.DropColumn(
                name: "OperationalStatus",
                table: "AchCycles");

            migrationBuilder.DropColumn(
                name: "ReceptionToleranceMinutes",
                table: "AchCycles");

            migrationBuilder.CreateIndex(
                name: "IX_NachaHeaders_ClearingHouseId",
                table: "NachaHeaders",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_EntryDetails_NachaID",
                table: "EntryDetails",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_BatchHeaders_NachaID",
                table: "BatchHeaders",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_AddendaRecords_NachaID",
                table: "AddendaRecords",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnCodes_ClearingHouseId_Code_FlowType_EffectiveFrom",
                table: "AchReturnCodes",
                columns: new[] { "ClearingHouseId", "Code", "FlowType", "EffectiveFrom" });

            migrationBuilder.AddForeignKey(
                name: "FK_AddendaRecords_NachaHeaders_NachaID",
                table: "AddendaRecords",
                column: "NachaID",
                principalTable: "NachaHeaders",
                principalColumn: "NachaID");

            migrationBuilder.AddForeignKey(
                name: "FK_BatchControls_NachaHeaders_NachaID",
                table: "BatchControls",
                column: "NachaID",
                principalTable: "NachaHeaders",
                principalColumn: "NachaID");

            migrationBuilder.AddForeignKey(
                name: "FK_BatchHeaders_NachaHeaders_NachaID",
                table: "BatchHeaders",
                column: "NachaID",
                principalTable: "NachaHeaders",
                principalColumn: "NachaID");

            migrationBuilder.AddForeignKey(
                name: "FK_EntryDetails_NachaHeaders_NachaID",
                table: "EntryDetails",
                column: "NachaID",
                principalTable: "NachaHeaders",
                principalColumn: "NachaID");

            migrationBuilder.AddForeignKey(
                name: "FK_FileControls_NachaHeaders_NachaID",
                table: "FileControls",
                column: "NachaID",
                principalTable: "NachaHeaders",
                principalColumn: "NachaID");
        }
    }
}
