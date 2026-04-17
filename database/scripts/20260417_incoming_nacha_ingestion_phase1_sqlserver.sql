-- SQL Server
IF OBJECT_ID(N'dbo.IncomingNachaFileIngestions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IncomingNachaFileIngestions (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        FileName nvarchar(260) NOT NULL,
        FileHashSha256 nvarchar(64) NOT NULL,
        FileSize bigint NOT NULL,
        ContentType nvarchar(120) NOT NULL,
        UploadedAtUtc datetime2 NOT NULL,
        ReceivedAtUtc datetime2 NULL,
        UploadedBy nvarchar(120) NOT NULL,
        ReceivedBy nvarchar(120) NULL,
        IngestionStatus nvarchar(40) NOT NULL,
        CycleResolutionStatus nvarchar(40) NOT NULL,
        ParsingStatus nvarchar(40) NOT NULL,
        DetectedClearingHouseId int NULL,
        ResolvedClearingHouseId int NULL,
        OperationalDate datetime2 NULL,
        ResolvedAchCycleId nvarchar(40) NULL,
        ResolutionMode nvarchar(60) NULL,
        ResolutionConfidence decimal(5,2) NULL,
        ResolutionEvidenceJson nvarchar(max) NOT NULL,
        RawStorageReference nvarchar(400) NULL,
        CorrelationId nvarchar(80) NOT NULL,
        ParentIngestionId uniqueidentifier NULL,
        IsReprocess bit NOT NULL CONSTRAINT DF_IncNacha_IsReprocess DEFAULT(0),
        Notes nvarchar(2000) NOT NULL CONSTRAINT DF_IncNacha_Notes DEFAULT(N''),
        WarningsJson nvarchar(max) NOT NULL CONSTRAINT DF_IncNacha_Warnings DEFAULT(N'[]'),
        CreatedAt datetime2 NULL,
        UpdatedAt datetime2 NULL,
        CONSTRAINT FK_IncNacha_Parent FOREIGN KEY (ParentIngestionId) REFERENCES dbo.IncomingNachaFileIngestions(Id)
    );
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncNacha_Hash_Size' AND object_id = OBJECT_ID('dbo.IncomingNachaFileIngestions'))
    DROP INDEX IX_IncNacha_Hash_Size ON dbo.IncomingNachaFileIngestions;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncNacha_BaseFingerprint_UQ' AND object_id = OBJECT_ID('dbo.IncomingNachaFileIngestions'))
    CREATE UNIQUE INDEX IX_IncNacha_BaseFingerprint_UQ ON dbo.IncomingNachaFileIngestions(FileHashSha256, FileSize) WHERE IsReprocess = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncNacha_ReprocessFingerprint_UQ' AND object_id = OBJECT_ID('dbo.IncomingNachaFileIngestions'))
    CREATE UNIQUE INDEX IX_IncNacha_ReprocessFingerprint_UQ ON dbo.IncomingNachaFileIngestions(ParentIngestionId, FileHashSha256, FileSize) WHERE IsReprocess = 1 AND ParentIngestionId IS NOT NULL;

IF OBJECT_ID(N'dbo.IncomingNachaFileProcessingResults', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IncomingNachaFileProcessingResults (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        IncomingNachaFileIngestionId uniqueidentifier NOT NULL,
        AttemptNumber int NOT NULL,
        StartedAtUtc datetime2 NOT NULL,
        FinishedAtUtc datetime2 NULL,
        TotalBatches int NOT NULL,
        TotalEntries int NOT NULL,
        TotalAddendas int NOT NULL,
        ValidCount int NOT NULL,
        InvalidCount int NOT NULL,
        WarningCount int NOT NULL,
        ErrorCount int NOT NULL,
        OutcomeStatus nvarchar(40) NOT NULL,
        FailureStage nvarchar(120) NOT NULL,
        ParserWarningsJson nvarchar(max) NOT NULL,
        ParserErrorsJson nvarchar(max) NOT NULL,
        IsReprocessable bit NOT NULL,
        CreatedAt datetime2 NULL,
        UpdatedAt datetime2 NULL,
        CONSTRAINT FK_IncNachaProc_Ingestion FOREIGN KEY (IncomingNachaFileIngestionId) REFERENCES dbo.IncomingNachaFileIngestions(Id) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncNachaProc_Attempt' AND object_id = OBJECT_ID('dbo.IncomingNachaFileProcessingResults'))
    CREATE UNIQUE INDEX IX_IncNachaProc_Attempt ON dbo.IncomingNachaFileProcessingResults(IncomingNachaFileIngestionId, AttemptNumber);

IF OBJECT_ID(N'dbo.IncomingNachaTransactionLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IncomingNachaTransactionLinks (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        IncomingNachaFileIngestionId uniqueidentifier NOT NULL,
        EntryDetailId int NULL,
        AddendaRecordId int NULL,
        AchTransactionId int NULL,
        LinkType nvarchar(30) NOT NULL,
        ConfidenceScore decimal(5,2) NOT NULL,
        EvidenceJson nvarchar(max) NOT NULL,
        LinkedAtUtc datetime2 NOT NULL,
        LinkedBy nvarchar(120) NOT NULL,
        IsFinal bit NOT NULL,
        CreatedAt datetime2 NULL,
        UpdatedAt datetime2 NULL,
        CONSTRAINT FK_IncNachaLink_Ingestion FOREIGN KEY (IncomingNachaFileIngestionId) REFERENCES dbo.IncomingNachaFileIngestions(Id) ON DELETE CASCADE
    );
END

IF COL_LENGTH('dbo.NachaHeaders', 'IncomingNachaFileIngestionId') IS NULL
    ALTER TABLE dbo.NachaHeaders ADD IncomingNachaFileIngestionId uniqueidentifier NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NachaHeaders_IncIngestion' AND object_id = OBJECT_ID('dbo.NachaHeaders'))
    CREATE INDEX IX_NachaHeaders_IncIngestion ON dbo.NachaHeaders(IncomingNachaFileIngestionId);
