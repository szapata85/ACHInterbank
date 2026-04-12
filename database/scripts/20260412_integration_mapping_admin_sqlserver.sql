IF OBJECT_ID('[dbo].[IntegrationMethods]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IntegrationMethods] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Code] NVARCHAR(150) NOT NULL UNIQUE,
        [DisplayName] NVARCHAR(200) NOT NULL,
        [SoapClientCode] NVARCHAR(120) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT(1),
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(120) NULL,
        [UpdatedBy] NVARCHAR(120) NULL
    );
END
GO

IF OBJECT_ID('[dbo].[IntegrationMethodParameters]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IntegrationMethodParameters] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [MethodId] INT NOT NULL,
        [ParameterPath] NVARCHAR(250) NOT NULL,
        [DisplayName] NVARCHAR(250) NOT NULL,
        [DataType] NVARCHAR(60) NOT NULL,
        [Cardinality] INT NOT NULL,
        [Required] BIT NOT NULL,
        [SortOrder] INT NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT(1),
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(120) NULL,
        [UpdatedBy] NVARCHAR(120) NULL,
        CONSTRAINT [FK_IntegrationMethodParameters_Method] FOREIGN KEY ([MethodId]) REFERENCES [dbo].[IntegrationMethods]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_IntegrationMethodParameters_Method_ParameterPath] UNIQUE ([MethodId],[ParameterPath])
    );
END
GO

IF OBJECT_ID('[dbo].[IntegrationSourceCatalogFields]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IntegrationSourceCatalogFields] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [MethodId] INT NULL,
        [SourceKind] INT NOT NULL,
        [EntityName] NVARCHAR(120) NOT NULL,
        [FieldPath] NVARCHAR(250) NOT NULL,
        [DisplayName] NVARCHAR(250) NOT NULL,
        [DataType] NVARCHAR(60) NOT NULL,
        [Cardinality] INT NOT NULL,
        [Nullable] BIT NOT NULL,
        [SortOrder] INT NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT(1),
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(120) NULL,
        [UpdatedBy] NVARCHAR(120) NULL,
        CONSTRAINT [FK_IntegrationSourceCatalogFields_Method] FOREIGN KEY ([MethodId]) REFERENCES [dbo].[IntegrationMethods]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_IntegrationSourceCatalogFields_Method_SourceKind_FieldPath] UNIQUE ([MethodId],[SourceKind],[FieldPath])
    );
END
GO

IF OBJECT_ID('[dbo].[IntegrationMappingSets]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IntegrationMappingSets] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [MethodId] INT NOT NULL,
        [Name] NVARCHAR(220) NOT NULL,
        [Version] INT NOT NULL,
        [Status] INT NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT(1),
        [Notes] NVARCHAR(3000) NOT NULL DEFAULT N'',
        [PublishedAtUtc] DATETIME2 NULL,
        [PublishedBy] NVARCHAR(120) NOT NULL DEFAULT N'',
        [ValidationSummaryJson] NVARCHAR(MAX) NOT NULL DEFAULT N'',
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(120) NULL,
        [UpdatedBy] NVARCHAR(120) NULL,
        CONSTRAINT [FK_IntegrationMappingSets_Method] FOREIGN KEY ([MethodId]) REFERENCES [dbo].[IntegrationMethods]([Id]) ON DELETE NO ACTION
    );
END
GO

IF OBJECT_ID('[dbo].[IntegrationMappingRules]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IntegrationMappingRules] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [MappingSetId] UNIQUEIDENTIFIER NOT NULL,
        [MethodId] INT NOT NULL,
        [ParameterId] BIGINT NOT NULL,
        [SourceKind] INT NOT NULL,
        [SourceCatalogFieldId] BIGINT NULL,
        [SourceFieldPath] NVARCHAR(300) NOT NULL DEFAULT N'',
        [FixedValue] NVARCHAR(1000) NULL,
        [DefaultValue] NVARCHAR(1000) NULL,
        [TransformationCode] NVARCHAR(80) NULL,
        [FormatMask] NVARCHAR(120) NULL,
        [Priority] INT NOT NULL,
        [RequiredOverride] BIT NULL,
        [Enabled] BIT NOT NULL DEFAULT(1),
        [ConditionExpression] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(120) NULL,
        [UpdatedBy] NVARCHAR(120) NULL,
        CONSTRAINT [FK_IntegrationMappingRules_Set] FOREIGN KEY ([MappingSetId]) REFERENCES [dbo].[IntegrationMappingSets]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_IntegrationMappingRules_Parameter] FOREIGN KEY ([ParameterId]) REFERENCES [dbo].[IntegrationMethodParameters]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_IntegrationMappingRules_SourceField] FOREIGN KEY ([SourceCatalogFieldId]) REFERENCES [dbo].[IntegrationSourceCatalogFields]([Id]) ON DELETE NO ACTION
    );
END
GO

IF OBJECT_ID('[dbo].[IntegrationMappingSetHistory]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IntegrationMappingSetHistory] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [MappingSetId] UNIQUEIDENTIFIER NOT NULL,
        [MethodId] INT NOT NULL,
        [Version] INT NOT NULL,
        [Status] INT NOT NULL,
        [Action] NVARCHAR(80) NOT NULL,
        [PerformedBy] NVARCHAR(120) NOT NULL,
        [PerformedAtUtc] DATETIME2 NOT NULL,
        [SnapshotJson] NVARCHAR(MAX) NOT NULL,
        [SnapshotHash] NVARCHAR(128) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(120) NULL,
        [UpdatedBy] NVARCHAR(120) NULL,
        CONSTRAINT [FK_IntegrationMappingSetHistory_Set] FOREIGN KEY ([MappingSetId]) REFERENCES [dbo].[IntegrationMappingSets]([Id]) ON DELETE CASCADE
    );
END
GO

IF COL_LENGTH('dbo.ContrapartidaDispatchBatches', 'MappingSetId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContrapartidaDispatchBatches] ADD [MappingSetId] UNIQUEIDENTIFIER NULL;
END
GO
IF COL_LENGTH('dbo.ContrapartidaDispatchBatches', 'MappingVersion') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContrapartidaDispatchBatches] ADD [MappingVersion] INT NULL;
END
GO
IF COL_LENGTH('dbo.ContrapartidaDispatchBatches', 'MappingSnapshotHash') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContrapartidaDispatchBatches] ADD [MappingSnapshotHash] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ContrapartidaDispatchBatches_MappingSnapshotHash] DEFAULT N'';
END
GO

CREATE INDEX [IX_IntegrationMethodParameters_MethodId_ParameterPath] ON [dbo].[IntegrationMethodParameters]([MethodId],[ParameterPath]);
CREATE INDEX [IX_IntegrationSourceCatalogFields_MethodId_SourceKind_FieldPath] ON [dbo].[IntegrationSourceCatalogFields]([MethodId],[SourceKind],[FieldPath]);
CREATE INDEX [IX_IntegrationMappingSets_MethodId_Status_Version] ON [dbo].[IntegrationMappingSets]([MethodId],[Status],[Version]);
CREATE INDEX [IX_IntegrationMappingRules_MappingSetId_ParameterId_Priority] ON [dbo].[IntegrationMappingRules]([MappingSetId],[ParameterId],[Priority]);
CREATE INDEX [IX_IntegrationMappingSetHistory_MappingSetId_PerformedAtUtc] ON [dbo].[IntegrationMappingSetHistory]([MappingSetId],[PerformedAtUtc]);
