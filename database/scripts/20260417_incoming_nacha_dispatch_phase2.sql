CREATE TABLE IF NOT EXISTS "IncomingNachaDispatchQueue" (
    "Id" uuid PRIMARY KEY,
    "IncomingNachaFileIngestionId" uuid NOT NULL,
    "IncomingNachaEntryClassificationId" uuid NOT NULL,
    "IncomingNachaTransactionLinkId" uuid NOT NULL,
    "AchTransactionId" integer NOT NULL,
    "AchCycleId" varchar(50) NOT NULL,
    "ClearingHouseId" integer NOT NULL,
    "OperationalDate" timestamp without time zone NOT NULL,
    "QueueStatus" integer NOT NULL,
    "Priority" integer NOT NULL DEFAULT 100,
    "IdempotencyDispatchKey" varchar(200) NOT NULL,
    "AttemptCount" integer NOT NULL DEFAULT 0,
    "NextAttemptAtUtc" timestamp without time zone NULL,
    "LastAttemptAtUtc" timestamp without time zone NULL,
    "LastErrorCode" varchar(80) NOT NULL DEFAULT '',
    "LastErrorMessage" varchar(4000) NOT NULL DEFAULT '',
    "LastResponseCode" varchar(80) NOT NULL DEFAULT '',
    "ConfirmedAtUtc" timestamp without time zone NULL,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT timezone('utc', now()),
    "CreatedBy" varchar(100) NOT NULL DEFAULT 'system',
    "UpdatedAt" timestamp without time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedBy" varchar(100) NOT NULL DEFAULT 'system'
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_IncomingNachaDispatchQueue_IdempotencyDispatchKey"
    ON "IncomingNachaDispatchQueue" ("IdempotencyDispatchKey");

CREATE INDEX IF NOT EXISTS "IX_IncomingNachaDispatchQueue_Status_NextAttempt_Priority"
    ON "IncomingNachaDispatchQueue" ("QueueStatus", "NextAttemptAtUtc", "Priority");

CREATE TABLE IF NOT EXISTS "IncomingNachaIntegrationExecution" (
    "Id" uuid PRIMARY KEY,
    "DispatchQueueId" uuid NOT NULL,
    "MethodName" varchar(120) NOT NULL,
    "MappingSetId" uuid NULL,
    "MappingVersion" integer NULL,
    "MappingSnapshotHash" varchar(200) NOT NULL DEFAULT '',
    "RequestHash" varchar(200) NOT NULL,
    "ResponseHash" varchar(200) NOT NULL DEFAULT '',
    "RequestPayloadXml" text NOT NULL DEFAULT '',
    "ResponsePayloadXml" text NOT NULL DEFAULT '',
    "ResponseCode" varchar(80) NOT NULL DEFAULT '',
    "ResponseMessage" varchar(4000) NOT NULL DEFAULT '',
    "IsSuccess" boolean NOT NULL DEFAULT FALSE,
    "IsRetryable" boolean NOT NULL DEFAULT FALSE,
    "StartedAtUtc" timestamp without time zone NOT NULL,
    "FinishedAtUtc" timestamp without time zone NULL,
    "CorrelationId" varchar(120) NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT timezone('utc', now()),
    "CreatedBy" varchar(100) NOT NULL DEFAULT 'system',
    "UpdatedAt" timestamp without time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedBy" varchar(100) NOT NULL DEFAULT 'system'
);

CREATE INDEX IF NOT EXISTS "IX_IncomingNachaIntegrationExecution_DispatchQueue_StartedAtUtc"
    ON "IncomingNachaIntegrationExecution" ("DispatchQueueId", "StartedAtUtc");
