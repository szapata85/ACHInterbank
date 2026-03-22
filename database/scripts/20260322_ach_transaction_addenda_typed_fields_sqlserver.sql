-- Remediación Registro Tipo 7: campos tipados por negocio en AchTransactionAddenda (SQL Server)

IF COL_LENGTH('AchTransactionAddenda', 'BusinessType') IS NULL
BEGIN
    ALTER TABLE AchTransactionAddenda ADD BusinessType VARCHAR(20) NOT NULL CONSTRAINT DF_AchTransactionAddenda_BusinessType DEFAULT ('Credit');
END

IF COL_LENGTH('AchTransactionAddenda', 'Purpose') IS NULL
BEGIN
    ALTER TABLE AchTransactionAddenda ADD Purpose VARCHAR(10) NULL;
END

IF COL_LENGTH('AchTransactionAddenda', 'Reference') IS NULL
BEGIN
    ALTER TABLE AchTransactionAddenda ADD Reference VARCHAR(53) NULL;
END

IF COL_LENGTH('AchTransactionAddenda', 'CollectorId') IS NULL
BEGIN
    ALTER TABLE AchTransactionAddenda ADD CollectorId VARCHAR(13) NULL;
END

IF COL_LENGTH('AchTransactionAddenda', 'ReceiverCustomerCode') IS NULL
BEGIN
    ALTER TABLE AchTransactionAddenda ADD ReceiverCustomerCode VARCHAR(30) NULL;
END

IF COL_LENGTH('AchTransactionAddenda', 'ServiceDescription') IS NULL
BEGIN
    ALTER TABLE AchTransactionAddenda ADD ServiceDescription VARCHAR(15) NULL;
END

IF COL_LENGTH('AchTransactionAddenda', 'ReturnReasonCode') IS NULL
BEGIN
    ALTER TABLE AchTransactionAddenda ADD ReturnReasonCode VARCHAR(4) NULL;
END

IF COL_LENGTH('AchTransactionAddenda', 'OriginalTraceNumber') IS NULL
BEGIN
    ALTER TABLE AchTransactionAddenda ADD OriginalTraceNumber VARCHAR(15) NULL;
END

IF COL_LENGTH('AchTransactionAddenda', 'NewTraceNumber') IS NULL
BEGIN
    ALTER TABLE AchTransactionAddenda ADD NewTraceNumber VARCHAR(15) NULL;
END
