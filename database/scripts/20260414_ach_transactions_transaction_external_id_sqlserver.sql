-- Migración progresiva: identificador operativo explícito (SQL Server)
-- No destructiva: conserva Reference como legado transicional.

IF COL_LENGTH('AchTransactions', 'TransactionExternalId') IS NULL
BEGIN
    ALTER TABLE AchTransactions ADD TransactionExternalId VARCHAR(64) NULL;
END

UPDATE AchTransactions
SET TransactionExternalId = Reference
WHERE (TransactionExternalId IS NULL OR LTRIM(RTRIM(TransactionExternalId)) = '')
  AND Reference IS NOT NULL
  AND LTRIM(RTRIM(Reference)) <> '';
