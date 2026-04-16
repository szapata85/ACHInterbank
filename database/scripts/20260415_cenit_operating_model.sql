-- CENIT operating governance model (PostgreSQL)
CREATE TABLE IF NOT EXISTS "CenitCycleExecutions" (
    "Id" BIGSERIAL PRIMARY KEY,
    "AchCycleId" varchar(40) NOT NULL UNIQUE,
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "CompletedAtUtc" timestamp with time zone NULL,
    "Status" varchar(30) NOT NULL,
    "Summary" varchar(500) NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    CONSTRAINT "FK_CenitCycleExecutions_AchCycles_AchCycleId" FOREIGN KEY ("AchCycleId") REFERENCES "AchCycles" ("Id") ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS "CenitNettingExecutions" (
    "Id" BIGSERIAL PRIMARY KEY,
    "CenitCycleExecutionId" bigint NOT NULL UNIQUE,
    "CalculatedAtUtc" timestamp with time zone NOT NULL,
    "TotalDebit" numeric(18,2) NOT NULL,
    "TotalCredit" numeric(18,2) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    CONSTRAINT "FK_CenitNettingExecutions_CenitCycleExecutions_CenitCycleExecutionId" FOREIGN KEY ("CenitCycleExecutionId") REFERENCES "CenitCycleExecutions" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "CenitNetPositions" (
    "Id" BIGSERIAL PRIMARY KEY,
    "CenitNettingExecutionId" bigint NOT NULL,
    "FinancialInstitutionId" integer NOT NULL,
    "DebitAmount" numeric(18,2) NOT NULL,
    "CreditAmount" numeric(18,2) NOT NULL,
    "NetAmount" numeric(18,2) NOT NULL,
    "AvailableLiquidity" numeric(18,2) NOT NULL,
    "HasInsufficientFunds" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    CONSTRAINT "FK_CenitNetPositions_CenitNettingExecutions_CenitNettingExecutionId" FOREIGN KEY ("CenitNettingExecutionId") REFERENCES "CenitNettingExecutions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CenitNetPositions_FinancialInstitutions_FinancialInstitutionId" FOREIGN KEY ("FinancialInstitutionId") REFERENCES "FinancialInstitutions" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_CenitNetPositions_CenitNettingExecutionId_FinancialInstitutionId" ON "CenitNetPositions" ("CenitNettingExecutionId", "FinancialInstitutionId");

CREATE TABLE IF NOT EXISTS "CenitNettingDetails" (
    "Id" BIGSERIAL PRIMARY KEY,
    "CenitNettingExecutionId" bigint NOT NULL,
    "AchTransactionId" integer NOT NULL,
    "SourceInstitutionId" integer NOT NULL,
    "DestinationInstitutionId" integer NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "IncludedInSettlement" boolean NOT NULL,
    "DecisionReason" varchar(150) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    CONSTRAINT "FK_CenitNettingDetails_CenitNettingExecutions_CenitNettingExecutionId" FOREIGN KEY ("CenitNettingExecutionId") REFERENCES "CenitNettingExecutions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CenitNettingDetails_AchTransactions_AchTransactionId" FOREIGN KEY ("AchTransactionId") REFERENCES "AchTransactions" ("Id") ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS "CenitCycleQueues" (
    "Id" BIGSERIAL PRIMARY KEY,
    "AchTransactionId" integer NOT NULL,
    "TargetAchCycleId" varchar(40) NOT NULL,
    "OriginalAchCycleId" varchar(40) NULL,
    "QueueReason" varchar(120) NOT NULL,
    "Status" varchar(30) NOT NULL,
    "EnqueuedAtUtc" timestamp with time zone NOT NULL,
    "DequeuedAtUtc" timestamp with time zone NULL,
    "CenitCycleExecutionId" bigint NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    CONSTRAINT "FK_CenitCycleQueues_AchTransactions_AchTransactionId" FOREIGN KEY ("AchTransactionId") REFERENCES "AchTransactions" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CenitCycleQueues_AchCycles_TargetAchCycleId" FOREIGN KEY ("TargetAchCycleId") REFERENCES "AchCycles" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CenitCycleQueues_CenitCycleExecutions_CenitCycleExecutionId" FOREIGN KEY ("CenitCycleExecutionId") REFERENCES "CenitCycleExecutions" ("Id") ON DELETE NO ACTION
);
CREATE INDEX IF NOT EXISTS "IX_CenitCycleQueues_TargetAchCycleId_Status" ON "CenitCycleQueues" ("TargetAchCycleId", "Status");

CREATE TABLE IF NOT EXISTS "LiquidityOptimizationDecisions" (
    "Id" BIGSERIAL PRIMARY KEY,
    "CenitCycleExecutionId" bigint NOT NULL,
    "AchTransactionId" integer NOT NULL,
    "DecisionType" varchar(30) NOT NULL,
    "Priority" integer NOT NULL,
    "DecisionReason" varchar(200) NOT NULL,
    "DecidedAtUtc" timestamp with time zone NOT NULL,
    "FromCycleId" varchar(40) NOT NULL,
    "ToCycleId" varchar(40) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    CONSTRAINT "FK_LiquidityOptimizationDecisions_CenitCycleExecutions_CenitCycleExecutionId" FOREIGN KEY ("CenitCycleExecutionId") REFERENCES "CenitCycleExecutions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LiquidityOptimizationDecisions_AchTransactions_AchTransactionId" FOREIGN KEY ("AchTransactionId") REFERENCES "AchTransactions" ("Id") ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS "ReturnOfReturnFlows" (
    "Id" BIGSERIAL PRIMARY KEY,
    "SourceReturnTransactionId" integer NOT NULL,
    "ReturnOfReturnTransactionId" integer NOT NULL,
    "ReasonCode" varchar(20) NOT NULL,
    "Status" varchar(30) NOT NULL,
    "OrchestratedAtUtc" timestamp with time zone NOT NULL,
    "CenitCycleExecutionId" bigint NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    CONSTRAINT "FK_ReturnOfReturnFlows_Source" FOREIGN KEY ("SourceReturnTransactionId") REFERENCES "AchTransactions" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ReturnOfReturnFlows_Return" FOREIGN KEY ("ReturnOfReturnTransactionId") REFERENCES "AchTransactions" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ReturnOfReturnFlows_CenitCycleExecutions" FOREIGN KEY ("CenitCycleExecutionId") REFERENCES "CenitCycleExecutions" ("Id") ON DELETE SET NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ReturnOfReturnFlows_SourceReturnTransactionId" ON "ReturnOfReturnFlows" ("SourceReturnTransactionId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ReturnOfReturnFlows_ReturnOfReturnTransactionId" ON "ReturnOfReturnFlows" ("ReturnOfReturnTransactionId");

ALTER TABLE "CenitNetPositions"
    ADD COLUMN IF NOT EXISTS "ExternalLiquidity" numeric(18,2) NULL,
    ADD COLUMN IF NOT EXISTS "SimulatedLiquidity" numeric(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "LiquiditySourceType" varchar(20) NOT NULL DEFAULT 'Simulated';

ALTER TABLE "CenitNettingDetails"
    ADD COLUMN IF NOT EXISTS "AchBatchId" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ValueDate" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    ADD COLUMN IF NOT EXISTS "ClearingHouseId" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ClearingHouseCode" varchar(16) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "SourceFileReference" varchar(120) NOT NULL DEFAULT '';

ALTER TABLE "LiquidityOptimizationDecisions"
    ADD COLUMN IF NOT EXISTS "AchBatchId" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ValueDate" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    ADD COLUMN IF NOT EXISTS "ClearingHouseId" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ClearingHouseCode" varchar(16) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "SourceFileReference" varchar(120) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "LiquidityModelUsed" varchar(20) NOT NULL DEFAULT 'Simulated';

CREATE TABLE IF NOT EXISTS "AchReturnCodes" (
    "Id" SERIAL PRIMARY KEY,
    "Code" varchar(10) NOT NULL UNIQUE,
    "Description" varchar(200) NOT NULL,
    "AppliesToDebit" boolean NOT NULL,
    "AppliesToCredit" boolean NOT NULL,
    "AppliesToPrenotification" boolean NOT NULL,
    "AppliesToReturn" boolean NOT NULL,
    "RequiresAddenda" boolean NOT NULL,
    "MaxDaysAllowed" integer NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "RegulatorySource" varchar(20) NOT NULL DEFAULT 'CENIT',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now())
);

CREATE TABLE IF NOT EXISTS "AchFileRejectionCodes" (
    "Id" SERIAL PRIMARY KEY,
    "Code" varchar(10) NOT NULL UNIQUE,
    "Description" varchar(200) NOT NULL,
    "Severity" varchar(20) NOT NULL,
    "AppliesToStage" varchar(30) NOT NULL,
    "IsRetryable" boolean NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now())
);

CREATE TABLE IF NOT EXISTS "AchTransactionTypePolicies" (
    "Id" SERIAL PRIMARY KEY,
    "TransactionType" varchar(30) NOT NULL UNIQUE,
    "PriorityOrder" integer NOT NULL,
    "IsMonetary" boolean NOT NULL,
    "RequiresPrenotification" boolean NOT NULL,
    "CanBeReturned" boolean NOT NULL,
    "CanBeReturnedAgain" boolean NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now())
);

CREATE TABLE IF NOT EXISTS "AchReturnPolicies" (
    "Id" SERIAL PRIMARY KEY,
    "TransactionType" varchar(30) NOT NULL,
    "AllowedReturnCodesCsv" varchar(500) NOT NULL,
    "MaxDays" integer NOT NULL,
    "RequiredOriginalTransactionState" varchar(40) NOT NULL DEFAULT '',
    "AllowsReturnOfReturn" boolean NOT NULL,
    "RequiresAddenda" boolean NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now())
);

CREATE TABLE IF NOT EXISTS "AchReturnOfReturnPolicies" (
    "Id" SERIAL PRIMARY KEY,
    "OriginalReturnCode" varchar(10) NOT NULL,
    "AllowedNewReturnCodesCsv" varchar(500) NOT NULL,
    "MaxDays" integer NOT NULL,
    "RequiredOriginalState" varchar(40) NOT NULL,
    "IsUniquePerTransaction" boolean NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now())
);

CREATE TABLE IF NOT EXISTS "AchPrenotificationPolicies" (
    "Id" SERIAL PRIMARY KEY,
    "TransactionType" varchar(30) NOT NULL UNIQUE,
    "IsRequired" boolean NOT NULL,
    "RequiresAddenda" boolean NOT NULL,
    "BlocksMonetaryTransactionIfMissing" boolean NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now())
);
