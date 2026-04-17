-- PostgreSQL
CREATE TABLE IF NOT EXISTS "IncomingNachaFileIngestions" (
    "Id" uuid PRIMARY KEY,
    "FileName" varchar(260) NOT NULL,
    "FileHashSha256" varchar(64) NOT NULL,
    "FileSize" bigint NOT NULL,
    "ContentType" varchar(120) NOT NULL,
    "UploadedAtUtc" timestamp without time zone NOT NULL,
    "ReceivedAtUtc" timestamp without time zone NULL,
    "UploadedBy" varchar(120) NOT NULL,
    "ReceivedBy" varchar(120) NULL,
    "IngestionStatus" varchar(40) NOT NULL,
    "CycleResolutionStatus" varchar(40) NOT NULL,
    "ParsingStatus" varchar(40) NOT NULL,
    "DetectedClearingHouseId" integer NULL,
    "ResolvedClearingHouseId" integer NULL,
    "OperationalDate" timestamp without time zone NULL,
    "ResolvedAchCycleId" varchar(40) NULL,
    "ResolutionMode" varchar(60) NULL,
    "ResolutionConfidence" numeric(5,2) NULL,
    "ResolutionEvidenceJson" text NOT NULL,
    "RawStorageReference" varchar(400) NULL,
    "CorrelationId" varchar(80) NOT NULL,
    "ParentIngestionId" uuid NULL,
    "IsReprocess" boolean NOT NULL DEFAULT false,
    "Notes" varchar(2000) NOT NULL DEFAULT '',
    "WarningsJson" text NOT NULL DEFAULT '[]',
    "CreatedAt" timestamp without time zone NULL,
    "UpdatedAt" timestamp without time zone NULL,
    CONSTRAINT "FK_IncNacha_Parent" FOREIGN KEY ("ParentIngestionId") REFERENCES "IncomingNachaFileIngestions"("Id") ON DELETE RESTRICT
);

DROP INDEX IF EXISTS "IX_IncNacha_Hash_Size";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_IncNacha_BaseFingerprint_UQ"
    ON "IncomingNachaFileIngestions" ("FileHashSha256", "FileSize")
    WHERE NOT "IsReprocess";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_IncNacha_ReprocessFingerprint_UQ"
    ON "IncomingNachaFileIngestions" ("ParentIngestionId", "FileHashSha256", "FileSize")
    WHERE "IsReprocess" AND "ParentIngestionId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_IncNacha_Operational" ON "IncomingNachaFileIngestions" ("ResolvedClearingHouseId", "OperationalDate", "ResolvedAchCycleId");

CREATE TABLE IF NOT EXISTS "IncomingNachaFileProcessingResults" (
    "Id" uuid PRIMARY KEY,
    "IncomingNachaFileIngestionId" uuid NOT NULL,
    "AttemptNumber" integer NOT NULL,
    "StartedAtUtc" timestamp without time zone NOT NULL,
    "FinishedAtUtc" timestamp without time zone NULL,
    "TotalBatches" integer NOT NULL,
    "TotalEntries" integer NOT NULL,
    "TotalAddendas" integer NOT NULL,
    "ValidCount" integer NOT NULL,
    "InvalidCount" integer NOT NULL,
    "WarningCount" integer NOT NULL,
    "ErrorCount" integer NOT NULL,
    "OutcomeStatus" varchar(40) NOT NULL,
    "FailureStage" varchar(120) NOT NULL,
    "ParserWarningsJson" text NOT NULL,
    "ParserErrorsJson" text NOT NULL,
    "IsReprocessable" boolean NOT NULL,
    "CreatedAt" timestamp without time zone NULL,
    "UpdatedAt" timestamp without time zone NULL,
    CONSTRAINT "FK_IncNachaProc_Ingestion" FOREIGN KEY ("IncomingNachaFileIngestionId") REFERENCES "IncomingNachaFileIngestions"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_IncNachaProc_Attempt" ON "IncomingNachaFileProcessingResults" ("IncomingNachaFileIngestionId", "AttemptNumber");

CREATE TABLE IF NOT EXISTS "IncomingNachaTransactionLinks" (
    "Id" uuid PRIMARY KEY,
    "IncomingNachaFileIngestionId" uuid NOT NULL,
    "EntryDetailId" integer NULL,
    "AddendaRecordId" integer NULL,
    "AchTransactionId" integer NULL,
    "LinkType" varchar(30) NOT NULL,
    "ConfidenceScore" numeric(5,2) NOT NULL,
    "EvidenceJson" text NOT NULL,
    "LinkedAtUtc" timestamp without time zone NOT NULL,
    "LinkedBy" varchar(120) NOT NULL,
    "IsFinal" boolean NOT NULL,
    "CreatedAt" timestamp without time zone NULL,
    "UpdatedAt" timestamp without time zone NULL,
    CONSTRAINT "FK_IncNachaLink_Ingestion" FOREIGN KEY ("IncomingNachaFileIngestionId") REFERENCES "IncomingNachaFileIngestions"("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "IncomingNachaEntryClassifications" (
    "Id" uuid PRIMARY KEY,
    "IncomingNachaFileIngestionId" uuid NOT NULL,
    "EntryDetailId" integer NOT NULL,
    "AddendaRecordId" integer NULL,
    "FunctionalClass" varchar(40) NOT NULL,
    "EligibilityStatus" varchar(30) NOT NULL,
    "RequiresLink" boolean NOT NULL,
    "RequiresManualResolution" boolean NOT NULL,
    "OriginalTraceRef" varchar(30) NULL,
    "ReturnReasonCode" varchar(10) NULL,
    "PrenoteStatus" varchar(30) NOT NULL,
    "BusinessMeaning" varchar(500) NOT NULL,
    "ClassifierVersion" varchar(40) NOT NULL,
    "ClassificationEvidenceJson" text NOT NULL,
    "CreatedAt" timestamp without time zone NULL,
    "UpdatedAt" timestamp without time zone NULL,
    CONSTRAINT "FK_IncNachaClass_Ingestion" FOREIGN KEY ("IncomingNachaFileIngestionId") REFERENCES "IncomingNachaFileIngestions"("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_IncNachaClass_Entry_Addenda_UQ"
    ON "IncomingNachaEntryClassifications" ("IncomingNachaFileIngestionId", "EntryDetailId", "AddendaRecordId");

CREATE TABLE IF NOT EXISTS "IncomingNachaProcessingEvents" (
    "Id" uuid PRIMARY KEY,
    "IncomingNachaFileIngestionId" uuid NOT NULL,
    "EntryDetailId" integer NULL,
    "AddendaRecordId" integer NULL,
    "AchTransactionId" integer NULL,
    "EventType" varchar(80) NOT NULL,
    "EventStatus" varchar(40) NOT NULL,
    "Message" varchar(2000) NOT NULL,
    "EvidenceJson" text NOT NULL,
    "OccurredAtUtc" timestamp without time zone NOT NULL,
    "RaisedBy" varchar(120) NOT NULL,
    "CreatedAt" timestamp without time zone NULL,
    "UpdatedAt" timestamp without time zone NULL,
    CONSTRAINT "FK_IncNachaEvents_Ingestion" FOREIGN KEY ("IncomingNachaFileIngestionId") REFERENCES "IncomingNachaFileIngestions"("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_IncNachaEvents_Ingestion_Occurred"
    ON "IncomingNachaProcessingEvents" ("IncomingNachaFileIngestionId", "OccurredAtUtc");

ALTER TABLE "NachaHeaders" ADD COLUMN IF NOT EXISTS "IncomingNachaFileIngestionId" uuid NULL;
CREATE INDEX IF NOT EXISTS "IX_NachaHeaders_IncIngestion" ON "NachaHeaders"("IncomingNachaFileIngestionId");
ALTER TABLE "NachaHeaders"
    ADD CONSTRAINT "FK_NachaHeaders_IncIngestion"
    FOREIGN KEY ("IncomingNachaFileIngestionId") REFERENCES "IncomingNachaFileIngestions"("Id") ON DELETE SET NULL;
