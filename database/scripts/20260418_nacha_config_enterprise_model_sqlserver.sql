-- NACHA-M enterprise configuration model (SQL Server)
-- Phase: persistence baseline + initial backfill from legacy NACHA tables

IF OBJECT_ID('dbo.CatClearingHouse', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatClearingHouse (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Code varchar(20) NOT NULL UNIQUE,
        Name varchar(120) NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_CatClearingHouse_IsActive DEFAULT 1,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatClearingHouse_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatClearingHouse_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.CatDirection', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatDirection (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Code varchar(20) NOT NULL UNIQUE,
        NameEs varchar(80) NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_CatDirection_IsActive DEFAULT 1,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatDirection_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatDirection_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.CatFlowType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatFlowType (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Code varchar(40) NOT NULL UNIQUE,
        NameEs varchar(100) NOT NULL,
        DirectionDefaultId int NULL,
        IsActive bit NOT NULL CONSTRAINT DF_CatFlowType_IsActive DEFAULT 1,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatFlowType_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatFlowType_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CatFlowType_Direction FOREIGN KEY (DirectionDefaultId) REFERENCES dbo.CatDirection(Id)
    );
END;

IF OBJECT_ID('dbo.CatConfigStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatConfigStatus (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Code varchar(20) NOT NULL UNIQUE,
        IsEditable bit NOT NULL,
        IsPublishable bit NOT NULL,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatConfigStatus_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatConfigStatus_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.CatServiceClass', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatServiceClass (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Code varchar(20) NOT NULL,
        NameEs varchar(120) NOT NULL,
        ClearingHouseId int NULL,
        IsActive bit NOT NULL CONSTRAINT DF_CatServiceClass_IsActive DEFAULT 1,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatServiceClass_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatServiceClass_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_CatServiceClass_CodeClearing UNIQUE(Code, ClearingHouseId),
        CONSTRAINT FK_CatServiceClass_Clearing FOREIGN KEY (ClearingHouseId) REFERENCES dbo.CatClearingHouse(Id)
    );
END;

IF OBJECT_ID('dbo.CatRecordCode', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatRecordCode (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Code varchar(5) NOT NULL UNIQUE,
        NameEs varchar(120) NOT NULL,
        IsMandatoryBase bit NOT NULL CONSTRAINT DF_CatRecordCode_Mandatory DEFAULT 0,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatRecordCode_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatRecordCode_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.CatDataSourceType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatDataSourceType (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Code varchar(40) NOT NULL UNIQUE,
        NameEs varchar(120) NOT NULL,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatDataSourceType_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatDataSourceType_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.CatRuleType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatRuleType (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Code varchar(40) NOT NULL UNIQUE,
        NameEs varchar(120) NOT NULL,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatRuleType_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_CatRuleType_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;

-- Config tables (subset DDL aligned to EF; for full constraints rely on migrations)
IF OBJECT_ID('dbo.CfgProfile', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CfgProfile (
        Id int IDENTITY(1,1) PRIMARY KEY,
        ProfileCode varchar(80) NOT NULL UNIQUE,
        NameEs varchar(160) NOT NULL,
        Description varchar(1000) NULL,
        ClearingHouseId int NOT NULL,
        FlowTypeId int NOT NULL,
        DirectionId int NOT NULL,
        ServiceClassId int NULL,
        ContextPriority int NOT NULL CONSTRAINT DF_CfgProfile_Priority DEFAULT 100,
        EffectiveFrom datetime2 NOT NULL,
        EffectiveTo datetime2 NULL,
        StatusId int NOT NULL,
        VersionMajor int NOT NULL CONSTRAINT DF_CfgProfile_VMajor DEFAULT 1,
        VersionMinor int NOT NULL CONSTRAINT DF_CfgProfile_VMinor DEFAULT 0,
        PublishedAt datetime2 NULL,
        PublishedBy varchar(120) NULL,
        SupersedesProfileId int NULL,
        RowVersion rowversion,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_CfgProfile_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_CfgProfile_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_CfgProfile_ContextVersion UNIQUE(ClearingHouseId, FlowTypeId, DirectionId, ServiceClassId, VersionMajor, VersionMinor),
        CONSTRAINT FK_CfgProfile_Clearing FOREIGN KEY (ClearingHouseId) REFERENCES dbo.CatClearingHouse(Id),
        CONSTRAINT FK_CfgProfile_Flow FOREIGN KEY (FlowTypeId) REFERENCES dbo.CatFlowType(Id),
        CONSTRAINT FK_CfgProfile_Direction FOREIGN KEY (DirectionId) REFERENCES dbo.CatDirection(Id),
        CONSTRAINT FK_CfgProfile_Service FOREIGN KEY (ServiceClassId) REFERENCES dbo.CatServiceClass(Id),
        CONSTRAINT FK_CfgProfile_Status FOREIGN KEY (StatusId) REFERENCES dbo.CatConfigStatus(Id),
        CONSTRAINT FK_CfgProfile_Supersedes FOREIGN KEY (SupersedesProfileId) REFERENCES dbo.CfgProfile(Id)
    );
END;

IF OBJECT_ID('dbo.HistConfigSnapshot', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistConfigSnapshot (
        Id int IDENTITY(1,1) PRIMARY KEY,
        ProfileId int NOT NULL,
        VersionMajor int NOT NULL,
        VersionMinor int NOT NULL,
        SnapshotType varchar(30) NOT NULL,
        SnapshotJson varchar(16000) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CreatedBy varchar(120) NOT NULL,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_HistConfigSnapshot_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_HistConfigSnapshot_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_HistConfigSnapshot_Profile FOREIGN KEY (ProfileId) REFERENCES dbo.CfgProfile(Id)
    );
END;

IF OBJECT_ID('dbo.HistConfigChange', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistConfigChange (
        Id int IDENTITY(1,1) PRIMARY KEY,
        ProfileId int NOT NULL,
        EntityName varchar(120) NOT NULL,
        EntityId varchar(120) NOT NULL,
        ChangeType varchar(40) NOT NULL,
        BeforeJson varchar(16000) NULL,
        AfterJson varchar(16000) NULL,
        ChangedAtUtc datetime2 NOT NULL,
        ChangedBy varchar(120) NOT NULL,
        CorrelationId varchar(120) NULL,
        CreatedAt datetimeoffset NOT NULL CONSTRAINT DF_HistConfigChange_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetimeoffset NOT NULL CONSTRAINT DF_HistConfigChange_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_HistConfigChange_Profile FOREIGN KEY (ProfileId) REFERENCES dbo.CfgProfile(Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CatClearingHouse WHERE Code = 'ACH') INSERT INTO dbo.CatClearingHouse(Code, Name, IsActive) VALUES ('ACH','ACH Colombia',1);
IF NOT EXISTS (SELECT 1 FROM dbo.CatClearingHouse WHERE Code = 'CENIT') INSERT INTO dbo.CatClearingHouse(Code, Name, IsActive) VALUES ('CENIT','CENIT',1);

IF NOT EXISTS (SELECT 1 FROM dbo.CatConfigStatus WHERE Code = 'BORRADOR') INSERT INTO dbo.CatConfigStatus(Code, IsEditable, IsPublishable) VALUES ('BORRADOR',1,1);
IF NOT EXISTS (SELECT 1 FROM dbo.CatConfigStatus WHERE Code = 'PUBLICADO') INSERT INTO dbo.CatConfigStatus(Code, IsEditable, IsPublishable) VALUES ('PUBLICADO',0,0);
IF NOT EXISTS (SELECT 1 FROM dbo.CatConfigStatus WHERE Code = 'INACTIVO') INSERT INTO dbo.CatConfigStatus(Code, IsEditable, IsPublishable) VALUES ('INACTIVO',0,0);
IF NOT EXISTS (SELECT 1 FROM dbo.CatConfigStatus WHERE Code = 'ARCHIVADO') INSERT INTO dbo.CatConfigStatus(Code, IsEditable, IsPublishable) VALUES ('ARCHIVADO',0,0);

-- Backfill real se resuelve con seeder C# para preservar reglas de mapeo.
