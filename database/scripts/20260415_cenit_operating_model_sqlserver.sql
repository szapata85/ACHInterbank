-- CENIT operating governance model (SQL Server)
IF OBJECT_ID('dbo.CenitCycleExecutions', 'U') IS NULL
BEGIN
CREATE TABLE dbo.CenitCycleExecutions (
    Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AchCycleId varchar(40) NOT NULL UNIQUE,
    StartedAtUtc datetime2 NOT NULL,
    CompletedAtUtc datetime2 NULL,
    Status varchar(30) NOT NULL,
    Summary varchar(500) NOT NULL DEFAULT '',
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CenitCycleExecutions_AchCycles FOREIGN KEY (AchCycleId) REFERENCES dbo.AchCycles(Id)
);
END

IF OBJECT_ID('dbo.CenitNettingExecutions', 'U') IS NULL
BEGIN
CREATE TABLE dbo.CenitNettingExecutions (
    Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CenitCycleExecutionId bigint NOT NULL UNIQUE,
    CalculatedAtUtc datetime2 NOT NULL,
    TotalDebit decimal(18,2) NOT NULL,
    TotalCredit decimal(18,2) NOT NULL,
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CenitNettingExecutions_Execution FOREIGN KEY (CenitCycleExecutionId) REFERENCES dbo.CenitCycleExecutions(Id) ON DELETE CASCADE
);
END

IF OBJECT_ID('dbo.CenitNetPositions', 'U') IS NULL
BEGIN
CREATE TABLE dbo.CenitNetPositions (
    Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CenitNettingExecutionId bigint NOT NULL,
    FinancialInstitutionId int NOT NULL,
    DebitAmount decimal(18,2) NOT NULL,
    CreditAmount decimal(18,2) NOT NULL,
    NetAmount decimal(18,2) NOT NULL,
    AvailableLiquidity decimal(18,2) NOT NULL,
    HasInsufficientFunds bit NOT NULL,
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CenitNetPositions_Execution FOREIGN KEY (CenitNettingExecutionId) REFERENCES dbo.CenitNettingExecutions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CenitNetPositions_FI FOREIGN KEY (FinancialInstitutionId) REFERENCES dbo.FinancialInstitutions(Id)
);
CREATE UNIQUE INDEX IX_CenitNetPositions_Execution_FI ON dbo.CenitNetPositions (CenitNettingExecutionId, FinancialInstitutionId);
END

IF OBJECT_ID('dbo.CenitNettingDetails', 'U') IS NULL
BEGIN
CREATE TABLE dbo.CenitNettingDetails (
    Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CenitNettingExecutionId bigint NOT NULL,
    AchTransactionId int NOT NULL,
    SourceInstitutionId int NOT NULL,
    DestinationInstitutionId int NOT NULL,
    Amount decimal(18,2) NOT NULL,
    IncludedInSettlement bit NOT NULL,
    DecisionReason varchar(150) NOT NULL,
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CenitNettingDetails_Execution FOREIGN KEY (CenitNettingExecutionId) REFERENCES dbo.CenitNettingExecutions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CenitNettingDetails_Transaction FOREIGN KEY (AchTransactionId) REFERENCES dbo.AchTransactions(Id)
);
END

IF OBJECT_ID('dbo.CenitCycleQueues', 'U') IS NULL
BEGIN
CREATE TABLE dbo.CenitCycleQueues (
    Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AchTransactionId int NOT NULL,
    TargetAchCycleId varchar(40) NOT NULL,
    OriginalAchCycleId varchar(40) NULL,
    QueueReason varchar(120) NOT NULL,
    Status varchar(30) NOT NULL,
    EnqueuedAtUtc datetime2 NOT NULL,
    DequeuedAtUtc datetime2 NULL,
    CenitCycleExecutionId bigint NULL,
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CenitCycleQueues_Transaction FOREIGN KEY (AchTransactionId) REFERENCES dbo.AchTransactions(Id),
    CONSTRAINT FK_CenitCycleQueues_Cycle FOREIGN KEY (TargetAchCycleId) REFERENCES dbo.AchCycles(Id),
    CONSTRAINT FK_CenitCycleQueues_Execution FOREIGN KEY (CenitCycleExecutionId) REFERENCES dbo.CenitCycleExecutions(Id)
);
CREATE INDEX IX_CenitCycleQueues_TargetCycle_Status ON dbo.CenitCycleQueues (TargetAchCycleId, Status);
END

IF OBJECT_ID('dbo.LiquidityOptimizationDecisions', 'U') IS NULL
BEGIN
CREATE TABLE dbo.LiquidityOptimizationDecisions (
    Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CenitCycleExecutionId bigint NOT NULL,
    AchTransactionId int NOT NULL,
    DecisionType varchar(30) NOT NULL,
    Priority int NOT NULL,
    DecisionReason varchar(200) NOT NULL,
    DecidedAtUtc datetime2 NOT NULL,
    FromCycleId varchar(40) NOT NULL,
    ToCycleId varchar(40) NULL,
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_LiquidityOptimizationDecisions_Execution FOREIGN KEY (CenitCycleExecutionId) REFERENCES dbo.CenitCycleExecutions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_LiquidityOptimizationDecisions_Transaction FOREIGN KEY (AchTransactionId) REFERENCES dbo.AchTransactions(Id)
);
END

IF OBJECT_ID('dbo.ReturnOfReturnFlows', 'U') IS NULL
BEGIN
CREATE TABLE dbo.ReturnOfReturnFlows (
    Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SourceReturnTransactionId int NOT NULL,
    ReturnOfReturnTransactionId int NOT NULL,
    ReasonCode varchar(20) NOT NULL,
    Status varchar(30) NOT NULL,
    OrchestratedAtUtc datetime2 NOT NULL,
    CenitCycleExecutionId bigint NULL,
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ReturnOfReturnFlows_Source FOREIGN KEY (SourceReturnTransactionId) REFERENCES dbo.AchTransactions(Id),
    CONSTRAINT FK_ReturnOfReturnFlows_Return FOREIGN KEY (ReturnOfReturnTransactionId) REFERENCES dbo.AchTransactions(Id),
    CONSTRAINT FK_ReturnOfReturnFlows_Execution FOREIGN KEY (CenitCycleExecutionId) REFERENCES dbo.CenitCycleExecutions(Id)
);
END
