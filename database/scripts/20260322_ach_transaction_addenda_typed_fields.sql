-- Remediación Registro Tipo 7: campos tipados por negocio en AchTransactionAddenda

ALTER TABLE AchTransactionAddenda
ADD COLUMN IF NOT EXISTS BusinessType VARCHAR(20) NOT NULL DEFAULT 'Credit';

ALTER TABLE AchTransactionAddenda
ADD COLUMN IF NOT EXISTS Purpose VARCHAR(10) NULL;

ALTER TABLE AchTransactionAddenda
ADD COLUMN IF NOT EXISTS Reference VARCHAR(53) NULL;

ALTER TABLE AchTransactionAddenda
ADD COLUMN IF NOT EXISTS CollectorId VARCHAR(13) NULL;

ALTER TABLE AchTransactionAddenda
ADD COLUMN IF NOT EXISTS ReceiverCustomerCode VARCHAR(30) NULL;

ALTER TABLE AchTransactionAddenda
ADD COLUMN IF NOT EXISTS ServiceDescription VARCHAR(15) NULL;

ALTER TABLE AchTransactionAddenda
ADD COLUMN IF NOT EXISTS ReturnReasonCode VARCHAR(4) NULL;

ALTER TABLE AchTransactionAddenda
ADD COLUMN IF NOT EXISTS OriginalTraceNumber VARCHAR(15) NULL;

ALTER TABLE AchTransactionAddenda
ADD COLUMN IF NOT EXISTS NewTraceNumber VARCHAR(15) NULL;
