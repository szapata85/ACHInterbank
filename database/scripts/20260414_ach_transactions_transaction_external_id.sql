-- Migración progresiva: identificador operativo explícito
-- No destructiva: conserva Reference como legado transicional.

ALTER TABLE AchTransactions
ADD COLUMN IF NOT EXISTS TransactionExternalId VARCHAR(64) NULL;

-- Backfill inicial para preservar idempotencia histórica en coexistencia.
UPDATE AchTransactions
SET TransactionExternalId = Reference
WHERE (TransactionExternalId IS NULL OR BTRIM(TransactionExternalId) = '')
  AND Reference IS NOT NULL
  AND BTRIM(Reference) <> '';
