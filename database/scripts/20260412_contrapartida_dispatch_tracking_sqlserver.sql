-- SQL Server
IF OBJECT_ID('dbo.ContrapartidaDispatchBatches', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContrapartidaDispatchBatches (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        AchCycleId nvarchar(32) NOT NULL,
        ClearingHouseId int NOT NULL,
        AchBatchId int NULL,
        Status nvarchar(30) NOT NULL,
        TriggerType nvarchar(30) NOT NULL,
        TriggeredAtUtc datetime2 NOT NULL,
        StartedAtUtc datetime2 NULL,
        FinishedAtUtc datetime2 NULL,
        TotalItems int NOT NULL CONSTRAINT DF_ContrapartidaDispatchBatches_TotalItems DEFAULT 0,
        TotalSucceeded int NOT NULL CONSTRAINT DF_ContrapartidaDispatchBatches_TotalSucceeded DEFAULT 0,
        TotalFailed int NOT NULL CONSTRAINT DF_ContrapartidaDispatchBatches_TotalFailed DEFAULT 0,
        TotalPartial int NOT NULL CONSTRAINT DF_ContrapartidaDispatchBatches_TotalPartial DEFAULT 0,
        RequestedBy nvarchar(120) NOT NULL,
        JobId nvarchar(150) NULL,
        RequestPayloadXml nvarchar(max) NOT NULL CONSTRAINT DF_ContrapartidaDispatchBatches_Request DEFAULT '',
        ResponsePayloadXml nvarchar(max) NOT NULL CONSTRAINT DF_ContrapartidaDispatchBatches_Response DEFAULT '',
        SummaryMessage nvarchar(2000) NOT NULL CONSTRAINT DF_ContrapartidaDispatchBatches_Summary DEFAULT '',
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_ContrapartidaDispatchBatches_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NOT NULL CONSTRAINT DF_ContrapartidaDispatchBatches_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID('dbo.ContrapartidaDispatchItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContrapartidaDispatchItems (
        Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AchTransactionId int NOT NULL,
        AchCycleId nvarchar(32) NOT NULL,
        ClearingHouseId int NOT NULL,
        AchBatchId int NOT NULL,
        State nvarchar(40) NOT NULL,
        NextAttemptAtUtc datetime2 NULL,
        LastAttemptAtUtc datetime2 NULL,
        LastSuccessAtUtc datetime2 NULL,
        AttemptCount int NOT NULL CONSTRAINT DF_ContrapartidaDispatchItems_AttemptCount DEFAULT 0,
        LastResponseCode nvarchar(20) NOT NULL CONSTRAINT DF_ContrapartidaDispatchItems_LastResponse DEFAULT '',
        LastErrorCode nvarchar(50) NOT NULL CONSTRAINT DF_ContrapartidaDispatchItems_LastErrorCode DEFAULT '',
        LastErrorMessage nvarchar(2000) NOT NULL CONSTRAINT DF_ContrapartidaDispatchItems_LastErrorMessage DEFAULT '',
        LastCorrelationId nvarchar(120) NOT NULL CONSTRAINT DF_ContrapartidaDispatchItems_LastCorrelation DEFAULT '',
        LastDispatchedBy nvarchar(120) NOT NULL CONSTRAINT DF_ContrapartidaDispatchItems_LastDispatchedBy DEFAULT '',
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_ContrapartidaDispatchItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NOT NULL CONSTRAINT DF_ContrapartidaDispatchItems_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_ContrapartidaDispatchItems_AchTransaction UNIQUE (AchTransactionId)
    );
END
GO

IF OBJECT_ID('dbo.ContrapartidaDispatchAttempts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContrapartidaDispatchAttempts (
        Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DispatchItemId bigint NOT NULL,
        DispatchBatchId uniqueidentifier NULL,
        AttemptNumber int NOT NULL,
        StartedAtUtc datetime2 NOT NULL,
        FinishedAtUtc datetime2 NULL,
        Result nvarchar(20) NOT NULL,
        CorrelationId nvarchar(120) NOT NULL,
        TriggeredBy nvarchar(120) NOT NULL,
        RetryEligible bit NOT NULL CONSTRAINT DF_ContrapartidaDispatchAttempts_RetryEligible DEFAULT 0,
        ExternalResponseCode nvarchar(20) NOT NULL CONSTRAINT DF_ContrapartidaDispatchAttempts_ResponseCode DEFAULT '',
        ExternalResponseMessage nvarchar(1000) NOT NULL CONSTRAINT DF_ContrapartidaDispatchAttempts_ResponseMessage DEFAULT '',
        ErrorCode nvarchar(50) NOT NULL CONSTRAINT DF_ContrapartidaDispatchAttempts_ErrorCode DEFAULT '',
        ErrorMessage nvarchar(2000) NOT NULL CONSTRAINT DF_ContrapartidaDispatchAttempts_ErrorMessage DEFAULT '',
        RequestPayloadXml nvarchar(max) NOT NULL CONSTRAINT DF_ContrapartidaDispatchAttempts_Request DEFAULT '',
        ResponsePayloadXml nvarchar(max) NOT NULL CONSTRAINT DF_ContrapartidaDispatchAttempts_Response DEFAULT '',
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_ContrapartidaDispatchAttempts_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NOT NULL CONSTRAINT DF_ContrapartidaDispatchAttempts_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_ContrapartidaDispatchAttempts_ItemAttempt UNIQUE (DispatchItemId, AttemptNumber)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ContrapartidaDispatchItems_AchTransactions')
ALTER TABLE dbo.ContrapartidaDispatchItems ADD CONSTRAINT FK_ContrapartidaDispatchItems_AchTransactions FOREIGN KEY (AchTransactionId) REFERENCES dbo.AchTransactions(Id) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ContrapartidaDispatchItems_AchCycles')
ALTER TABLE dbo.ContrapartidaDispatchItems ADD CONSTRAINT FK_ContrapartidaDispatchItems_AchCycles FOREIGN KEY (AchCycleId) REFERENCES dbo.AchCycles(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ContrapartidaDispatchItems_ClearingHouses')
ALTER TABLE dbo.ContrapartidaDispatchItems ADD CONSTRAINT FK_ContrapartidaDispatchItems_ClearingHouses FOREIGN KEY (ClearingHouseId) REFERENCES dbo.ClearingHouses(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ContrapartidaDispatchItems_AchBatches')
ALTER TABLE dbo.ContrapartidaDispatchItems ADD CONSTRAINT FK_ContrapartidaDispatchItems_AchBatches FOREIGN KEY (AchBatchId) REFERENCES dbo.AchBatches(Id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ContrapartidaDispatchBatches_AchCycles')
ALTER TABLE dbo.ContrapartidaDispatchBatches ADD CONSTRAINT FK_ContrapartidaDispatchBatches_AchCycles FOREIGN KEY (AchCycleId) REFERENCES dbo.AchCycles(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ContrapartidaDispatchBatches_ClearingHouses')
ALTER TABLE dbo.ContrapartidaDispatchBatches ADD CONSTRAINT FK_ContrapartidaDispatchBatches_ClearingHouses FOREIGN KEY (ClearingHouseId) REFERENCES dbo.ClearingHouses(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ContrapartidaDispatchBatches_AchBatches')
ALTER TABLE dbo.ContrapartidaDispatchBatches ADD CONSTRAINT FK_ContrapartidaDispatchBatches_AchBatches FOREIGN KEY (AchBatchId) REFERENCES dbo.AchBatches(Id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ContrapartidaDispatchAttempts_Items')
ALTER TABLE dbo.ContrapartidaDispatchAttempts ADD CONSTRAINT FK_ContrapartidaDispatchAttempts_Items FOREIGN KEY (DispatchItemId) REFERENCES dbo.ContrapartidaDispatchItems(Id) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ContrapartidaDispatchAttempts_Batches')
ALTER TABLE dbo.ContrapartidaDispatchAttempts ADD CONSTRAINT FK_ContrapartidaDispatchAttempts_Batches FOREIGN KEY (DispatchBatchId) REFERENCES dbo.ContrapartidaDispatchBatches(Id) ON DELETE SET NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ContrapartidaDispatchItems_StateNextAttempt')
CREATE INDEX IX_ContrapartidaDispatchItems_StateNextAttempt ON dbo.ContrapartidaDispatchItems(State, NextAttemptAtUtc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ContrapartidaDispatchItems_CycleState')
CREATE INDEX IX_ContrapartidaDispatchItems_CycleState ON dbo.ContrapartidaDispatchItems(ClearingHouseId, AchCycleId, State);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ContrapartidaDispatchBatches_CycleTriggered')
CREATE INDEX IX_ContrapartidaDispatchBatches_CycleTriggered ON dbo.ContrapartidaDispatchBatches(ClearingHouseId, AchCycleId, TriggeredAtUtc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ContrapartidaDispatchAttempts_BatchCreated')
CREATE INDEX IX_ContrapartidaDispatchAttempts_BatchCreated ON dbo.ContrapartidaDispatchAttempts(DispatchBatchId, CreatedAt);
GO
