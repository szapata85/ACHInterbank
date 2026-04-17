IF OBJECT_ID(N'[IncomingNachaDispatchQueue]', N'U') IS NULL
BEGIN
    CREATE TABLE [IncomingNachaDispatchQueue](
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [IncomingNachaFileIngestionId] uniqueidentifier NOT NULL,
        [IncomingNachaEntryClassificationId] uniqueidentifier NOT NULL,
        [IncomingNachaTransactionLinkId] uniqueidentifier NOT NULL,
        [AchTransactionId] int NOT NULL,
        [AchCycleId] nvarchar(50) NOT NULL,
        [ClearingHouseId] int NOT NULL,
        [OperationalDate] datetime2 NOT NULL,
        [QueueStatus] int NOT NULL,
        [Priority] int NOT NULL CONSTRAINT [DF_IncomingNachaDispatchQueue_Priority] DEFAULT (100),
        [IdempotencyDispatchKey] nvarchar(200) NOT NULL,
        [AttemptCount] int NOT NULL CONSTRAINT [DF_IncomingNachaDispatchQueue_AttemptCount] DEFAULT (0),
        [NextAttemptAtUtc] datetime2 NULL,
        [LastAttemptAtUtc] datetime2 NULL,
        [LastErrorCode] nvarchar(80) NOT NULL CONSTRAINT [DF_IncomingNachaDispatchQueue_LastErrorCode] DEFAULT (N''),
        [LastErrorMessage] nvarchar(4000) NOT NULL CONSTRAINT [DF_IncomingNachaDispatchQueue_LastErrorMessage] DEFAULT (N''),
        [LastResponseCode] nvarchar(80) NOT NULL CONSTRAINT [DF_IncomingNachaDispatchQueue_LastResponseCode] DEFAULT (N''),
        [ConfirmedAtUtc] datetime2 NULL,
        [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_IncomingNachaDispatchQueue_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL CONSTRAINT [DF_IncomingNachaDispatchQueue_CreatedBy] DEFAULT (N'system'),
        [UpdatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_IncomingNachaDispatchQueue_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] nvarchar(100) NOT NULL CONSTRAINT [DF_IncomingNachaDispatchQueue_UpdatedBy] DEFAULT (N'system')
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_IncomingNachaDispatchQueue_IdempotencyDispatchKey' AND object_id = OBJECT_ID('IncomingNachaDispatchQueue'))
    CREATE UNIQUE INDEX [UX_IncomingNachaDispatchQueue_IdempotencyDispatchKey] ON [IncomingNachaDispatchQueue]([IdempotencyDispatchKey]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncomingNachaDispatchQueue_Status_NextAttempt_Priority' AND object_id = OBJECT_ID('IncomingNachaDispatchQueue'))
    CREATE INDEX [IX_IncomingNachaDispatchQueue_Status_NextAttempt_Priority] ON [IncomingNachaDispatchQueue]([QueueStatus], [NextAttemptAtUtc], [Priority]);
GO

IF OBJECT_ID(N'[IncomingNachaIntegrationExecution]', N'U') IS NULL
BEGIN
    CREATE TABLE [IncomingNachaIntegrationExecution](
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [DispatchQueueId] uniqueidentifier NOT NULL,
        [MethodName] nvarchar(120) NOT NULL,
        [MappingSetId] uniqueidentifier NULL,
        [MappingVersion] int NULL,
        [MappingSnapshotHash] nvarchar(200) NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_MappingSnapshotHash] DEFAULT (N''),
        [RequestHash] nvarchar(200) NOT NULL,
        [ResponseHash] nvarchar(200) NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_ResponseHash] DEFAULT (N''),
        [RequestPayloadXml] nvarchar(max) NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_RequestPayloadXml] DEFAULT (N''),
        [ResponsePayloadXml] nvarchar(max) NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_ResponsePayloadXml] DEFAULT (N''),
        [ResponseCode] nvarchar(80) NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_ResponseCode] DEFAULT (N''),
        [ResponseMessage] nvarchar(4000) NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_ResponseMessage] DEFAULT (N''),
        [IsSuccess] bit NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_IsSuccess] DEFAULT (0),
        [IsRetryable] bit NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_IsRetryable] DEFAULT (0),
        [StartedAtUtc] datetime2 NOT NULL,
        [FinishedAtUtc] datetime2 NULL,
        [CorrelationId] nvarchar(120) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_CreatedBy] DEFAULT (N'system'),
        [UpdatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] nvarchar(100) NOT NULL CONSTRAINT [DF_IncomingNachaIntegrationExecution_UpdatedBy] DEFAULT (N'system')
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncomingNachaIntegrationExecution_DispatchQueue_StartedAtUtc' AND object_id = OBJECT_ID('IncomingNachaIntegrationExecution'))
    CREATE INDEX [IX_IncomingNachaIntegrationExecution_DispatchQueue_StartedAtUtc] ON [IncomingNachaIntegrationExecution]([DispatchQueueId], [StartedAtUtc]);
GO
