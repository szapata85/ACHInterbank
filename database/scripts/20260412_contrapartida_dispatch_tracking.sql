-- PostgreSQL
CREATE TABLE IF NOT EXISTS "ContrapartidaDispatchBatches" (
    "Id" uuid PRIMARY KEY,
    "AchCycleId" varchar(32) NOT NULL,
    "ClearingHouseId" integer NOT NULL,
    "AchBatchId" integer NULL,
    "Status" varchar(30) NOT NULL,
    "TriggerType" varchar(30) NOT NULL,
    "TriggeredAtUtc" timestamp with time zone NOT NULL,
    "StartedAtUtc" timestamp with time zone NULL,
    "FinishedAtUtc" timestamp with time zone NULL,
    "TotalItems" integer NOT NULL DEFAULT 0,
    "TotalSucceeded" integer NOT NULL DEFAULT 0,
    "TotalFailed" integer NOT NULL DEFAULT 0,
    "TotalPartial" integer NOT NULL DEFAULT 0,
    "RequestedBy" varchar(120) NOT NULL,
    "JobId" varchar(150) NULL,
    "RequestPayloadXml" text NOT NULL DEFAULT '',
    "ResponsePayloadXml" text NOT NULL DEFAULT '',
    "SummaryMessage" varchar(2000) NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now())
);

CREATE TABLE IF NOT EXISTS "ContrapartidaDispatchItems" (
    "Id" bigserial PRIMARY KEY,
    "AchTransactionId" integer NOT NULL UNIQUE,
    "AchCycleId" varchar(32) NOT NULL,
    "ClearingHouseId" integer NOT NULL,
    "AchBatchId" integer NOT NULL,
    "State" varchar(40) NOT NULL,
    "NextAttemptAtUtc" timestamp with time zone NULL,
    "LastAttemptAtUtc" timestamp with time zone NULL,
    "LastSuccessAtUtc" timestamp with time zone NULL,
    "AttemptCount" integer NOT NULL DEFAULT 0,
    "LastResponseCode" varchar(20) NOT NULL DEFAULT '',
    "LastErrorCode" varchar(50) NOT NULL DEFAULT '',
    "LastErrorMessage" varchar(2000) NOT NULL DEFAULT '',
    "LastCorrelationId" varchar(120) NOT NULL DEFAULT '',
    "LastDispatchedBy" varchar(120) NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now())
);

CREATE TABLE IF NOT EXISTS "ContrapartidaDispatchAttempts" (
    "Id" bigserial PRIMARY KEY,
    "DispatchItemId" bigint NOT NULL,
    "DispatchBatchId" uuid NULL,
    "AttemptNumber" integer NOT NULL,
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "FinishedAtUtc" timestamp with time zone NULL,
    "Result" varchar(20) NOT NULL,
    "CorrelationId" varchar(120) NOT NULL,
    "TriggeredBy" varchar(120) NOT NULL,
    "RetryEligible" boolean NOT NULL DEFAULT false,
    "ExternalResponseCode" varchar(20) NOT NULL DEFAULT '',
    "ExternalResponseMessage" varchar(1000) NOT NULL DEFAULT '',
    "ErrorCode" varchar(50) NOT NULL DEFAULT '',
    "ErrorMessage" varchar(2000) NOT NULL DEFAULT '',
    "RequestPayloadXml" text NOT NULL DEFAULT '',
    "ResponsePayloadXml" text NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    CONSTRAINT "FK_ContrapartidaDispatchAttempts_Items" FOREIGN KEY ("DispatchItemId") REFERENCES "ContrapartidaDispatchItems"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ContrapartidaDispatchAttempts_Batches" FOREIGN KEY ("DispatchBatchId") REFERENCES "ContrapartidaDispatchBatches"("Id") ON DELETE SET NULL,
    CONSTRAINT "UQ_ContrapartidaDispatchAttempts_ItemAttempt" UNIQUE ("DispatchItemId", "AttemptNumber")
);

CREATE INDEX IF NOT EXISTS "IX_ContrapartidaDispatchItems_StateNextAttempt" ON "ContrapartidaDispatchItems" ("State", "NextAttemptAtUtc");
CREATE INDEX IF NOT EXISTS "IX_ContrapartidaDispatchItems_CycleState" ON "ContrapartidaDispatchItems" ("ClearingHouseId", "AchCycleId", "State");
CREATE INDEX IF NOT EXISTS "IX_ContrapartidaDispatchBatches_CycleTriggered" ON "ContrapartidaDispatchBatches" ("ClearingHouseId", "AchCycleId", "TriggeredAtUtc");
CREATE INDEX IF NOT EXISTS "IX_ContrapartidaDispatchAttempts_BatchCreated" ON "ContrapartidaDispatchAttempts" ("DispatchBatchId", "CreatedAt");

ALTER TABLE "ContrapartidaDispatchItems"
    ADD CONSTRAINT "FK_ContrapartidaDispatchItems_AchTransactions" FOREIGN KEY ("AchTransactionId") REFERENCES "AchTransactions"("Id") ON DELETE CASCADE;
ALTER TABLE "ContrapartidaDispatchItems"
    ADD CONSTRAINT "FK_ContrapartidaDispatchItems_AchCycles" FOREIGN KEY ("AchCycleId") REFERENCES "AchCycles"("Id") ON DELETE RESTRICT;
ALTER TABLE "ContrapartidaDispatchItems"
    ADD CONSTRAINT "FK_ContrapartidaDispatchItems_ClearingHouses" FOREIGN KEY ("ClearingHouseId") REFERENCES "ClearingHouses"("Id") ON DELETE RESTRICT;
ALTER TABLE "ContrapartidaDispatchItems"
    ADD CONSTRAINT "FK_ContrapartidaDispatchItems_AchBatches" FOREIGN KEY ("AchBatchId") REFERENCES "AchBatches"("Id") ON DELETE RESTRICT;

ALTER TABLE "ContrapartidaDispatchBatches"
    ADD CONSTRAINT "FK_ContrapartidaDispatchBatches_AchCycles" FOREIGN KEY ("AchCycleId") REFERENCES "AchCycles"("Id") ON DELETE RESTRICT;
ALTER TABLE "ContrapartidaDispatchBatches"
    ADD CONSTRAINT "FK_ContrapartidaDispatchBatches_ClearingHouses" FOREIGN KEY ("ClearingHouseId") REFERENCES "ClearingHouses"("Id") ON DELETE RESTRICT;
ALTER TABLE "ContrapartidaDispatchBatches"
    ADD CONSTRAINT "FK_ContrapartidaDispatchBatches_AchBatches" FOREIGN KEY ("AchBatchId") REFERENCES "AchBatches"("Id") ON DELETE RESTRICT;
