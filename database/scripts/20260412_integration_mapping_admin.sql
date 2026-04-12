CREATE TABLE IF NOT EXISTS "IntegrationMethods" (
    "Id" SERIAL PRIMARY KEY,
    "Code" VARCHAR(150) NOT NULL UNIQUE,
    "DisplayName" VARCHAR(200) NOT NULL,
    "SoapClientCode" VARCHAR(120) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NULL,
    "CreatedBy" VARCHAR(120) NULL,
    "UpdatedBy" VARCHAR(120) NULL
);

CREATE TABLE IF NOT EXISTS "IntegrationMethodParameters" (
    "Id" BIGSERIAL PRIMARY KEY,
    "MethodId" INT NOT NULL REFERENCES "IntegrationMethods"("Id") ON DELETE CASCADE,
    "ParameterPath" VARCHAR(250) NOT NULL,
    "DisplayName" VARCHAR(250) NOT NULL,
    "DataType" VARCHAR(60) NOT NULL,
    "Cardinality" INT NOT NULL,
    "Required" BOOLEAN NOT NULL,
    "SortOrder" INT NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NULL,
    "CreatedBy" VARCHAR(120) NULL,
    "UpdatedBy" VARCHAR(120) NULL,
    UNIQUE("MethodId", "ParameterPath")
);

CREATE TABLE IF NOT EXISTS "IntegrationSourceCatalogFields" (
    "Id" BIGSERIAL PRIMARY KEY,
    "MethodId" INT NULL REFERENCES "IntegrationMethods"("Id") ON DELETE CASCADE,
    "SourceKind" INT NOT NULL,
    "EntityName" VARCHAR(120) NOT NULL,
    "FieldPath" VARCHAR(250) NOT NULL,
    "DisplayName" VARCHAR(250) NOT NULL,
    "DataType" VARCHAR(60) NOT NULL,
    "Cardinality" INT NOT NULL,
    "Nullable" BOOLEAN NOT NULL,
    "SortOrder" INT NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NULL,
    "CreatedBy" VARCHAR(120) NULL,
    "UpdatedBy" VARCHAR(120) NULL,
    UNIQUE("MethodId", "SourceKind", "FieldPath")
);

CREATE TABLE IF NOT EXISTS "IntegrationMappingSets" (
    "Id" UUID PRIMARY KEY,
    "MethodId" INT NOT NULL REFERENCES "IntegrationMethods"("Id") ON DELETE RESTRICT,
    "Name" VARCHAR(220) NOT NULL,
    "Version" INT NOT NULL,
    "Status" INT NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Notes" VARCHAR(3000) NOT NULL DEFAULT '',
    "PublishedAtUtc" TIMESTAMPTZ NULL,
    "PublishedBy" VARCHAR(120) NOT NULL DEFAULT '',
    "ValidationSummaryJson" TEXT NOT NULL DEFAULT '',
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NULL,
    "CreatedBy" VARCHAR(120) NULL,
    "UpdatedBy" VARCHAR(120) NULL
);

CREATE TABLE IF NOT EXISTS "IntegrationMappingRules" (
    "Id" BIGSERIAL PRIMARY KEY,
    "MappingSetId" UUID NOT NULL REFERENCES "IntegrationMappingSets"("Id") ON DELETE CASCADE,
    "MethodId" INT NOT NULL,
    "ParameterId" BIGINT NOT NULL REFERENCES "IntegrationMethodParameters"("Id") ON DELETE RESTRICT,
    "SourceKind" INT NOT NULL,
    "SourceCatalogFieldId" BIGINT NULL REFERENCES "IntegrationSourceCatalogFields"("Id") ON DELETE RESTRICT,
    "SourceFieldPath" VARCHAR(300) NOT NULL DEFAULT '',
    "FixedValue" VARCHAR(1000) NULL,
    "DefaultValue" VARCHAR(1000) NULL,
    "TransformationCode" VARCHAR(80) NULL,
    "FormatMask" VARCHAR(120) NULL,
    "Priority" INT NOT NULL,
    "RequiredOverride" BOOLEAN NULL,
    "Enabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "ConditionExpression" VARCHAR(500) NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NULL,
    "CreatedBy" VARCHAR(120) NULL,
    "UpdatedBy" VARCHAR(120) NULL
);

CREATE TABLE IF NOT EXISTS "IntegrationMappingSetHistory" (
    "Id" UUID PRIMARY KEY,
    "MappingSetId" UUID NOT NULL REFERENCES "IntegrationMappingSets"("Id") ON DELETE CASCADE,
    "MethodId" INT NOT NULL,
    "Version" INT NOT NULL,
    "Status" INT NOT NULL,
    "Action" VARCHAR(80) NOT NULL,
    "PerformedBy" VARCHAR(120) NOT NULL,
    "PerformedAtUtc" TIMESTAMPTZ NOT NULL,
    "SnapshotJson" TEXT NOT NULL,
    "SnapshotHash" VARCHAR(128) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NULL,
    "CreatedBy" VARCHAR(120) NULL,
    "UpdatedBy" VARCHAR(120) NULL
);

ALTER TABLE "ContrapartidaDispatchBatches"
    ADD COLUMN IF NOT EXISTS "MappingSetId" UUID NULL,
    ADD COLUMN IF NOT EXISTS "MappingVersion" INT NULL,
    ADD COLUMN IF NOT EXISTS "MappingSnapshotHash" VARCHAR(128) NOT NULL DEFAULT '';

CREATE INDEX IF NOT EXISTS "IX_IntegrationMethodParameters_MethodId_ParameterPath" ON "IntegrationMethodParameters" ("MethodId", "ParameterPath");
CREATE INDEX IF NOT EXISTS "IX_IntegrationSourceCatalogFields_MethodId_SourceKind_FieldPath" ON "IntegrationSourceCatalogFields" ("MethodId", "SourceKind", "FieldPath");
CREATE INDEX IF NOT EXISTS "IX_IntegrationMappingSets_MethodId_Status_Version" ON "IntegrationMappingSets" ("MethodId", "Status", "Version");
CREATE INDEX IF NOT EXISTS "IX_IntegrationMappingRules_MappingSetId_ParameterId_Priority" ON "IntegrationMappingRules" ("MappingSetId", "ParameterId", "Priority");
CREATE INDEX IF NOT EXISTS "IX_IntegrationMappingSetHistory_MappingSetId_PerformedAtUtc" ON "IntegrationMappingSetHistory" ("MappingSetId", "PerformedAtUtc");
