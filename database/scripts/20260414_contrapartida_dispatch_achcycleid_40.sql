-- Ajuste de compatibilidad:
-- AchCycle.Id es varchar(40) (hash SHA1 hex de 40 chars), por lo que
-- las tablas de despacho a contrapartida deben aceptar el mismo tamaño.

ALTER TABLE IF EXISTS "ContrapartidaDispatchItems"
    ALTER COLUMN "AchCycleId" TYPE VARCHAR(40);

ALTER TABLE IF EXISTS "ContrapartidaDispatchBatches"
    ALTER COLUMN "AchCycleId" TYPE VARCHAR(40);
